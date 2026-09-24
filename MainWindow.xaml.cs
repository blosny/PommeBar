using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

namespace PommeBar;

public partial class MainWindow : Window
{
    private readonly MediaController _mediaController;
    private static readonly string ConfigPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "position.cfg");
    private readonly System.Windows.Threading.DispatcherTimer _timelineTimer;
    private double _currentPositionSeconds = 0;
    private double _totalDurationSeconds = 100;
    private bool _isPlaying = false;

    public MainWindow()
    {
        InitializeComponent();

        _mediaController = new MediaController();
        _mediaController.OnMediaChanged += MediaController_OnMediaChanged;
        _mediaController.OnPlaybackStateChanged += MediaController_OnPlaybackStateChanged;
        _mediaController.OnTimelineChanged += MediaController_OnTimelineChanged;

        _timelineTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timelineTimer.Tick += (s, e) =>
        {
            if (_isPlaying && _currentPositionSeconds < _totalDurationSeconds)
            {
                _currentPositionSeconds += 1;
                TrackProgressBar.Value = _currentPositionSeconds;
            }
        };
        _timelineTimer.Start();
        
        this.Loaded += async (s, e) =>
        {
            try
            {
                string savedPos = "right";
                if (System.IO.File.Exists(ConfigPath))
                {
                    savedPos = System.IO.File.ReadAllText(ConfigPath).Trim().ToLower();
                }
                else
                {
                    // Prompt user on first run where to position on taskbar
                    var dlg = new PositionDialog();
                    dlg.ShowDialog();
                    savedPos = dlg.SelectedPosition;
                }
                ApplyPosition(savedPos);

                MenuStartup.IsChecked = StartupManager.IsStartupEnabled();
                await _mediaController.InitializeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in Loaded: {ex}");
            }
        };
    }

    private void ApplyPosition(string position)
    {
        var workArea = SystemParameters.WorkArea;
        double w = this.ActualWidth > 0 ? this.ActualWidth : 430;
        double h = this.ActualHeight > 0 ? this.ActualHeight : 70;

        switch (position)
        {
            case "left":
                this.Left = workArea.Left + 20;
                break;
            case "center":
                this.Left = (workArea.Width - w) / 2;
                break;
            case "right":
            default:
                this.Left = workArea.Right - w - 20;
                break;
        }
        this.Top = workArea.Bottom - h - 10;

        try { System.IO.File.WriteAllText(ConfigPath, position); } catch { }
    }

    private void MenuChoosePos_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new PositionDialog();
        if (dlg.ShowDialog() == true)
        {
            ApplyPosition(dlg.SelectedPosition);
        }
    }

    private void MenuPosRight_Click(object sender, RoutedEventArgs e) => ApplyPosition("right");
    private void MenuPosCenter_Click(object sender, RoutedEventArgs e) => ApplyPosition("center");
    private void MenuPosLeft_Click(object sender, RoutedEventArgs e) => ApplyPosition("left");

    private void MenuStartup_Click(object sender, RoutedEventArgs e)
    {
        bool isEnabled = MenuStartup.IsChecked;
        bool result = StartupManager.SetStartup(isEnabled);
        if (!result)
        {
            MenuStartup.IsChecked = !isEnabled;
        }
    }

    private void MenuGitHub_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/blosny/PommeBar",
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void MenuExit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void MediaController_OnMediaChanged(string title, string artist, BitmapImage? albumArt)
    {
        try
        {
            SongTitleText.Text = title;
            ArtistNameText.Text = artist;

            if (albumArt != null)
            {
                AlbumArtImage.Source = albumArt;
                AlbumArtImage.Visibility = Visibility.Visible;
                AlbumArtPlaceholder.Visibility = Visibility.Collapsed;
            }
            else
            {
                AlbumArtImage.Source = null;
                AlbumArtImage.Visibility = Visibility.Collapsed;
                AlbumArtPlaceholder.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnMediaChanged ERROR: {ex}");
        }
    }

    private void MediaController_OnPlaybackStateChanged(bool isPlaying)
    {
        try
        {
            _isPlaying = isPlaying;
            BtnPlayPause.Icon = new Wpf.Ui.Controls.SymbolIcon
            {
                Symbol = isPlaying ? Wpf.Ui.Controls.SymbolRegular.Pause24 : Wpf.Ui.Controls.SymbolRegular.Play24
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnPlaybackStateChanged ERROR: {ex}");
        }
    }

    private void MediaController_OnTimelineChanged(double currentSeconds, double totalSeconds)
    {
        if (totalSeconds > 0)
        {
            _currentPositionSeconds = currentSeconds;
            _totalDurationSeconds = totalSeconds;
            TrackProgressBar.Maximum = totalSeconds;
            TrackProgressBar.Value = currentSeconds;
        }
    }

    private async void BtnPrevious_Click(object sender, RoutedEventArgs e)
    {
        await _mediaController.SkipPreviousAsync();
    }

    private async void BtnPlayPause_Click(object sender, RoutedEventArgs e)
    {
        await _mediaController.TogglePlayPauseAsync();
    }

    private async void BtnNext_Click(object sender, RoutedEventArgs e)
    {
        await _mediaController.SkipNextAsync();
    }

    private async void BtnHeart_Click(object sender, RoutedEventArgs e)
    {
        BtnHeart.IsEnabled = false;
        try
        {
            await AppleMusicAutomation.ToggleFavoriteAsync();
        }
        finally
        {
            BtnHeart.IsEnabled = true;
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            this.DragMove();
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

    private void SongInfo_Click(object sender, MouseButtonEventArgs e)
    {
        try
        {
            var proc = System.Diagnostics.Process.GetProcessesByName("AppleMusic").FirstOrDefault();
            if (proc != null && proc.MainWindowHandle != IntPtr.Zero)
            {
                ShowWindow(proc.MainWindowHandle, 9); // SW_RESTORE
                SetForegroundWindow(proc.MainWindowHandle);
            }
        }
        catch { }
    }

    private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        const byte VK_VOLUME_UP = 0xAF;
        const byte VK_VOLUME_DOWN = 0xAE;
        const uint KEYEVENTF_KEYUP = 0x0002;

        byte key = e.Delta > 0 ? VK_VOLUME_UP : VK_VOLUME_DOWN;
        keybd_event(key, 0, 0, 0);
        keybd_event(key, 0, KEYEVENTF_KEYUP, 0);
    }
}