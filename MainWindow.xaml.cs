using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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
    private bool _isLocked = false;
    private bool _isExpanded = false;
    private bool _isUserSeeking = false;
    private string _currentPositionMode = "right";

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
                if (!_isUserSeeking)
                {
                    TimelineSlider.Value = _currentPositionSeconds;
                    TimeCurrentText.Text = FormatTime(_currentPositionSeconds);
                }
            }
        };
        _timelineTimer.Start();
        
        this.Loaded += async (s, e) =>
        {
            try
            {
                LoadSettings();

                bool startup = StartupManager.IsStartupEnabled();
                MenuStartup.IsChecked = startup;
                ChkStartup.IsChecked = startup;

                SyncVolumeUI();
                await _mediaController.InitializeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in Loaded: {ex}");
            }
        };
    }

    private void ClampToWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        double h = this.Height > 0 ? this.Height : 70;
        double w = this.Width > 0 ? this.Width : 450;

        // Never allow sinking below the taskbar
        double maxTop = workArea.Bottom - h - 10;
        if (this.Top > maxTop)
        {
            this.Top = maxTop;
        }
        if (this.Top < workArea.Top + 10)
        {
            this.Top = workArea.Top + 10;
        }

        // Never allow going off-screen horizontally
        double maxLeft = workArea.Right - w - 10;
        if (this.Left > maxLeft)
        {
            this.Left = maxLeft;
        }
        if (this.Left < workArea.Left + 10)
        {
            this.Left = workArea.Left + 10;
        }
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var lines = File.ReadAllLines(ConfigPath);
                if (lines.Length > 0 && !string.IsNullOrWhiteSpace(lines[0]))
                {
                    _currentPositionMode = lines[0].Trim().ToLower();
                }
                if (lines.Length > 1 && bool.TryParse(lines[1], out bool locked))
                {
                    SetPositionLock(locked, save: false);
                }
                if (lines.Length >= 4 && _currentPositionMode == "custom" 
                    && double.TryParse(lines[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double l) 
                    && double.TryParse(lines[3], NumberStyles.Any, CultureInfo.InvariantCulture, out double t))
                {
                    this.Left = l;
                    this.Top = t;
                    ClampToWorkArea();
                    return;
                }
            }
            else
            {
                // First run: prompt user for initial position
                var dlg = new PositionDialog();
                dlg.ShowDialog();
                _currentPositionMode = dlg.SelectedPosition;
            }
        }
        catch { }

        ApplyPosition(_currentPositionMode);
    }

    private void SaveSettings()
    {
        try
        {
            ClampToWorkArea();
            string content = $"{_currentPositionMode}\n{_isLocked}\n{this.Left.ToString(CultureInfo.InvariantCulture)}\n{this.Top.ToString(CultureInfo.InvariantCulture)}";
            File.WriteAllText(ConfigPath, content);
        }
        catch { }
    }

    private void ApplyPosition(string position)
    {
        _currentPositionMode = position;
        var workArea = SystemParameters.WorkArea;
        double w = this.Width > 0 ? this.Width : 450;
        double h = 70;

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
        ClampToWorkArea();
        SaveSettings();
    }

    private void SetPositionLock(bool locked, bool save = true)
    {
        _isLocked = locked;
        MenuLockPos.IsChecked = locked;

        var symbol = locked ? SymbolRegular.LockClosed24 : SymbolRegular.LockOpen24;
        string tip = locked ? "Konum Kilitli (Sabit)" : "Konum Kilidi Açık (Taşınabilir)";

        BtnLock.Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = symbol };
        BtnLock.ToolTip = tip;

        if (BtnLockCompact != null)
        {
            BtnLockCompact.Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = symbol };
            BtnLockCompact.ToolTip = tip;
            BtnLockCompact.Foreground = locked 
                ? new SolidColorBrush(Color.FromRgb(255, 59, 48)) 
                : new SolidColorBrush(Color.FromArgb(180, 255, 255, 255));
        }

        if (save)
        {
            SaveSettings();
        }
    }

    private void BtnLock_Click(object sender, RoutedEventArgs e)
    {
        SetPositionLock(!_isLocked);
    }

    private void MenuLockPos_Click(object sender, RoutedEventArgs e)
    {
        SetPositionLock(MenuLockPos.IsChecked);
    }

    private void BtnExpand_Click(object sender, RoutedEventArgs e)
    {
        _isExpanded = !_isExpanded;
        var workArea = SystemParameters.WorkArea;
        double targetHeight = _isExpanded ? 225 : 70;

        // Keep the bottom edge anchored above the taskbar
        double currentBottom = this.Top + (this.ActualHeight > 0 ? this.ActualHeight : this.Height);
        if (currentBottom > workArea.Bottom - 5 || currentBottom < workArea.Top + 100)
        {
            currentBottom = workArea.Bottom - 10;
        }

        if (_isExpanded)
        {
            ExpandedPanel.Visibility = Visibility.Visible;
            TrackProgressBar.Visibility = Visibility.Collapsed;
            BtnExpand.Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = SymbolRegular.ChevronDown24 };
            BtnExpand.ToolTip = "Daralt";
            SyncVolumeUI();
        }
        else
        {
            ExpandedPanel.Visibility = Visibility.Collapsed;
            TrackProgressBar.Visibility = Visibility.Visible;
            BtnExpand.Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = SymbolRegular.ChevronUp24 };
            BtnExpand.ToolTip = "Genişletilmiş Görünüm";
        }

        this.Height = targetHeight;
        this.Top = currentBottom - targetHeight;
        ClampToWorkArea();
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
        ChkStartup.IsChecked = MenuStartup.IsChecked;
    }

    private void ChkStartup_Click(object sender, RoutedEventArgs e)
    {
        bool isEnabled = ChkStartup.IsChecked == true;
        bool result = StartupManager.SetStartup(isEnabled);
        if (!result)
        {
            ChkStartup.IsChecked = !isEnabled;
        }
        MenuStartup.IsChecked = ChkStartup.IsChecked == true;
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
            ExpandedTitleText.Text = title;
            ExpandedArtistText.Text = artist;

            if (albumArt != null)
            {
                AlbumArtImage.Source = albumArt;
                AlbumArtImage.Visibility = Visibility.Visible;
                AlbumArtPlaceholder.Visibility = Visibility.Collapsed;

                ExpandedArtImage.Source = albumArt;
                ExpandedArtImage.Visibility = Visibility.Visible;
                ExpandedArtPlaceholder.Visibility = Visibility.Collapsed;
            }
            else
            {
                AlbumArtImage.Source = null;
                AlbumArtImage.Visibility = Visibility.Collapsed;
                AlbumArtPlaceholder.Visibility = Visibility.Visible;

                ExpandedArtImage.Source = null;
                ExpandedArtImage.Visibility = Visibility.Collapsed;
                ExpandedArtPlaceholder.Visibility = Visibility.Visible;
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
                Symbol = isPlaying ? SymbolRegular.Pause24 : SymbolRegular.Play24
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

            if (!_isUserSeeking)
            {
                TimelineSlider.Maximum = totalSeconds;
                TimelineSlider.Value = currentSeconds;
                TimeCurrentText.Text = FormatTime(currentSeconds);
                TimeTotalText.Text = FormatTime(totalSeconds);
            }
        }
    }

    private static string FormatTime(double seconds)
    {
        if (seconds < 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) return "0:00";
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1 
            ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}" 
            : $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}";
    }

    private void TimelineSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isUserSeeking = true;
    }

    private async void TimelineSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isUserSeeking = false;
        _currentPositionSeconds = TimelineSlider.Value;
        TimeCurrentText.Text = FormatTime(_currentPositionSeconds);
        TrackProgressBar.Value = _currentPositionSeconds;
        await _mediaController.SeekAsync(TimelineSlider.Value);
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (VolumeLabel == null) return;
        float level = (float)(VolumeSlider.Value / 100.0);
        VolumeController.SetVolume(level);
        VolumeLabel.Text = $"{(int)VolumeSlider.Value}%";
    }

    private void SyncVolumeUI()
    {
        if (VolumeSlider == null || VolumeLabel == null) return;
        float vol = VolumeController.GetVolume();
        VolumeSlider.Value = vol * 100.0;
        VolumeLabel.Text = $"{(int)(vol * 100)}%";
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
        if (_isLocked) return;

        if (e.ChangedButton == MouseButton.Left)
        {
            this.DragMove();
            _currentPositionMode = "custom";
            ClampToWorkArea();
            SaveSettings();
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

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
        float delta = e.Delta > 0 ? 0.02f : -0.02f;
        VolumeController.ChangeVolume(delta);
        SyncVolumeUI();
    }
}