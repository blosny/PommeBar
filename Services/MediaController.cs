using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;
using PommeBar.Models;

namespace PommeBar;

public class MediaController
{
    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private readonly HashSet<GlobalSystemMediaTransportControlsSession> _hookedSessions = new();

    public event Action<string, string, BitmapImage?, MediaAppType, string, string>? OnMediaChanged;
    public event Action<bool>? OnPlaybackStateChanged;
    public event Action<double, double>? OnTimelineChanged;

    public MediaAppType CurrentAppType { get; private set; } = MediaAppType.Unknown;
    public string CurrentAppName { get; private set; } = "PommeBar";
    public string CurrentAccentColor { get; private set; } = "#FF3B30";

    public async Task InitializeAsync()
    {
        try
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            
            if (_sessionManager != null)
            {
                _sessionManager.CurrentSessionChanged += SessionManager_CurrentSessionChanged;
                _sessionManager.SessionsChanged += SessionManager_SessionsChanged;
                HookAllSessions();
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

    private void SessionManager_SessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
    {
        HookAllSessions();
        UpdateCurrentSession();
    }

    private void HookAllSessions()
    {
        if (_sessionManager == null) return;
        try
        {
            var sessions = _sessionManager.GetSessions();
            if (sessions == null) return;

            foreach (var session in sessions)
            {
                if (!_hookedSessions.Contains(session))
                {
                    _hookedSessions.Add(session);
                    session.PlaybackInfoChanged += (s, e) =>
                    {
                        try
                        {
                            var info = s.GetPlaybackInfo();
                            if (info?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                            {
                                UpdateCurrentSession();
                            }
                        }
                        catch { }
                    };
                }
            }
        }
        catch { }
    }

    private GlobalSystemMediaTransportControlsSession? PickBestSession()
    {
        if (_sessionManager == null) return null;

        IReadOnlyList<GlobalSystemMediaTransportControlsSession>? sessions = null;
        try
        {
            sessions = _sessionManager.GetSessions();
        }
        catch { }

        if (sessions == null || sessions.Count == 0)
        {
            return _sessionManager.GetCurrentSession();
        }

        // 1. Prioritize any session that is currently PLAYING
        foreach (var s in sessions)
        {
            try
            {
                var info = s.GetPlaybackInfo();
                if (info?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                {
                    return s;
                }
            }
            catch { }
        }

        // 2. Prioritize dedicated music services (Spotify, Apple Music) even if paused
        foreach (var s in sessions)
        {
            try
            {
                string id = (s.SourceAppUserModelId ?? "").ToLowerInvariant();
                if (id.Contains("spotify") || id.Contains("applemusic") || id.Contains("apple"))
                {
                    return s;
                }
            }
            catch { }
        }

        // 3. Fallback to Windows default current session
        return _sessionManager.GetCurrentSession() ?? sessions.FirstOrDefault();
    }

    private void UpdateCurrentSession()
    {
        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged -= CurrentSession_MediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= CurrentSession_PlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged -= CurrentSession_TimelinePropertiesChanged;
        }

        _currentSession = PickBestSession();

        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged += CurrentSession_MediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged += CurrentSession_PlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged += CurrentSession_TimelinePropertiesChanged;
            
            var (type, name, color) = ParseSourceApp(_currentSession.SourceAppUserModelId);
            CurrentAppType = type;
            CurrentAppName = name;
            CurrentAccentColor = color;

            _ = UpdateMediaPropertiesAsync();
            UpdatePlaybackInfo();
            UpdateTimelineInfo();
        }
        else
        {
            CurrentAppType = MediaAppType.Unknown;
            CurrentAppName = "PommeBar";
            CurrentAccentColor = "#FF3B30";

            App.Current.Dispatcher.Invoke(() =>
            {
                OnMediaChanged?.Invoke("Müzik Bekleniyor...", "PommeBar", null, CurrentAppType, CurrentAppName, CurrentAccentColor);
                OnPlaybackStateChanged?.Invoke(false);
            });
        }
    }

    public static (MediaAppType type, string displayName, string accentColorHex) ParseSourceApp(string? appId)
    {
        if (string.IsNullOrEmpty(appId))
            return (MediaAppType.Unknown, "PommeBar", "#FF3B30");

        string id = appId.ToLowerInvariant();
        if (id.Contains("spotify"))
        {
            return (MediaAppType.Spotify, "Spotify", "#1DB954");
        }
        if (id.Contains("applemusic") || id.Contains("apple"))
        {
            return (MediaAppType.AppleMusic, "Apple Music", "#FF3B30");
        }
        if (id.Contains("chrome") || id.Contains("msedge") || id.Contains("firefox") || id.Contains("opera") || id.Contains("brave"))
        {
            return (MediaAppType.Browser, "Tarayıcı", "#0A84FF");
        }

        return (MediaAppType.Other, "Medya", "#FF3B30");
    }

    private async void CurrentSession_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        await UpdateMediaPropertiesAsync();
    }

    private void CurrentSession_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        UpdatePlaybackInfo();
    }

    private void CurrentSession_TimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
    {
        UpdateTimelineInfo();
    }

    public void UpdateTimelineInfo()
    {
        if (_currentSession == null) return;
        try
        {
            var timeline = _currentSession.GetTimelineProperties();
            if (timeline != null)
            {
                double current = timeline.Position.TotalSeconds;
                double total = timeline.EndTime.TotalSeconds;
                App.Current.Dispatcher.Invoke(() =>
                {
                    OnTimelineChanged?.Invoke(current, total);
                });
            }
        }
        catch { }
    }

    private async Task UpdateMediaPropertiesAsync()
    {
        if (_currentSession == null) return;

        try
        {
            var properties = await _currentSession.TryGetMediaPropertiesAsync();
            if (properties == null) return;

            string title = string.IsNullOrEmpty(properties.Title) ? "Müzik Bekleniyor..." : properties.Title;
            string artist = string.IsNullOrEmpty(properties.Artist) ? "Apple Music veya Spotify Başlatın" : properties.Artist;
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
                OnMediaChanged?.Invoke(title, artist, albumArt, CurrentAppType, CurrentAppName, CurrentAccentColor);
            });
        }
        catch (Exception ex)
        {
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
        {
            try
            {
                var info = _currentSession.GetPlaybackInfo();
                if (info != null)
                {
                    if (info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                    {
                        var ok = await _currentSession.TryPauseAsync();
                        if (!ok) await _currentSession.TryTogglePlayPauseAsync();
                    }
                    else
                    {
                        var ok = await _currentSession.TryPlayAsync();
                        if (!ok) await _currentSession.TryTogglePlayPauseAsync();
                    }
                }
                else
                {
                    await _currentSession.TryTogglePlayPauseAsync();
                }
            }
            catch
            {
                await _currentSession.TryTogglePlayPauseAsync();
            }
        }
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

    public async Task SeekAsync(double seconds)
    {
        if (_currentSession != null)
        {
            try
            {
                long ticks = (long)(seconds * 10_000_000);
                await _currentSession.TryChangePlaybackPositionAsync(ticks);
            }
            catch { }
        }
    }
}
