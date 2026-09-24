using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace PommeBar;

public class MediaController
{
    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;

    public event Action<string, string, BitmapImage?>? OnMediaChanged;
    public event Action<bool>? OnPlaybackStateChanged;

    public async Task InitializeAsync()
    {
        try
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            
            if (_sessionManager != null)
            {
                _sessionManager.CurrentSessionChanged += SessionManager_CurrentSessionChanged;
                UpdateCurrentSession();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to initialize MediaController: {ex.Message}");
        }
    }

    private void SessionManager_CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        UpdateCurrentSession();
    }

    private void UpdateCurrentSession()
    {
        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged -= CurrentSession_MediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= CurrentSession_PlaybackInfoChanged;
        }

        _currentSession = _sessionManager?.GetCurrentSession();

        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged += CurrentSession_MediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged += CurrentSession_PlaybackInfoChanged;
            
            _ = UpdateMediaPropertiesAsync();
            UpdatePlaybackInfo();
        }
        else
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                OnMediaChanged?.Invoke("Müzik Bekleniyor...", "PommeBar", null);
                OnPlaybackStateChanged?.Invoke(false);
            });
        }
    }

    private async void CurrentSession_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        await UpdateMediaPropertiesAsync();
    }

    private void CurrentSession_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        UpdatePlaybackInfo();
    }

    private async Task UpdateMediaPropertiesAsync()
    {
        if (_currentSession == null) return;

        try
        {
            var properties = await _currentSession.TryGetMediaPropertiesAsync();
            if (properties == null) return;

            string title = string.IsNullOrEmpty(properties.Title) ? "Bilinmeyen Şarkı" : properties.Title;
            string artist = string.IsNullOrEmpty(properties.Artist) ? "Bilinmeyen Sanatçı" : properties.Artist;
            BitmapImage? albumArt = null;

            if (properties.Thumbnail != null)
            {
                try
                {
                    using var stream = await properties.Thumbnail.OpenReadAsync();
                    if (stream != null && stream.Size > 0)
                    {
                        var buffer = new Windows.Storage.Streams.Buffer((uint)stream.Size);
                        await stream.ReadAsync(buffer, buffer.Capacity, InputStreamOptions.None);
                        
                        using var reader = DataReader.FromBuffer(buffer);
                        byte[] fileBytes = new byte[buffer.Length];
                        reader.ReadBytes(fileBytes);
                        
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                var ms = new MemoryStream(fileBytes);
                                var image = new BitmapImage();
                                image.BeginInit();
                                image.CacheOption = BitmapCacheOption.OnLoad;
                                image.StreamSource = ms;
                                image.EndInit();
                                image.Freeze();
                                albumArt = image;
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Failed to decode album art bitmap: {ex.Message}");
                            }
                        });
                    }
                }
                catch (Exception thumbEx)
                {
                    Debug.WriteLine($"Thumbnail error: {thumbEx.Message}");
                }
            }

            App.Current.Dispatcher.Invoke(() =>
            {
                OnMediaChanged?.Invoke(title, artist, albumArt);
            });
        }
        catch (Exception ex)
        {
            try { System.IO.File.AppendAllText(@"C:\projects\pomme-bar\app.log", $"[{DateTime.Now}] Error getting media properties: {ex}\n"); } catch { }
            Debug.WriteLine($"Error getting media properties: {ex.Message}");
        }
    }

    private void UpdatePlaybackInfo()
    {
        if (_currentSession == null) return;

        var playbackInfo = _currentSession.GetPlaybackInfo();
        bool isPlaying = playbackInfo?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

        App.Current.Dispatcher.Invoke(() =>
        {
            OnPlaybackStateChanged?.Invoke(isPlaying);
        });
    }

    public async Task TogglePlayPauseAsync()
    {
        if (_currentSession != null)
            await _currentSession.TryTogglePlayPauseAsync();
    }

    public async Task SkipNextAsync()
    {
        if (_currentSession != null)
            await _currentSession.TrySkipNextAsync();
    }

    public async Task SkipPreviousAsync()
    {
        if (_currentSession != null)
            await _currentSession.TrySkipPreviousAsync();
    }
}
