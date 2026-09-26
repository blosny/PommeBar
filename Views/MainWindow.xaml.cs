using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

using PommeBar.Models;
using PommeBar.Services;

namespace PommeBar;

public partial class MainWindow : Window
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    private readonly MediaController _mediaController;
    private readonly TrayIconManager _trayManager;
    private readonly System.Windows.Threading.DispatcherTimer _timelineTimer;
    private readonly System.Windows.Threading.DispatcherTimer _superLockTimer;
    
    private double _currentPositionSeconds = 0;
    private double _totalDurationSeconds = 100;
    private bool _isPlaying = false;
    private bool _isExpanded = false;
    private bool _isUserSeeking = false;
    
    private string _currentPositionPreset = "left";
    private DockMode _currentDockMode = DockMode.Floating;
    private LockMode _currentLockMode = LockMode.Free;
    private MediaAppType _currentAppType = MediaAppType.Unknown;

    public MainWindow()
    {
        InitializeComponent();

        _trayManager = new TrayIconManager(this);
        _trayManager.OnLeftClick += ToggleVisibility;
        _trayManager.OnRightClick += () =>
        {
            if (this.ContextMenu != null)
            {
                this.ContextMenu.IsOpen = true;
            }
        };

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

        _superLockTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _superLockTimer.Tick += (s, e) =>
        {
            if (_currentLockMode == LockMode.SuperLocked)
            {
                EnforceTopmost();
            }
        };

        this.Loaded += async (s, e) =>
        {
            try
            {
                _trayManager.Initialize();
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

        this.Closed += (s, e) =>
        {
            _trayManager.Dispose();
        };
    }

    private void ToggleVisibility()
    {
        if (this.Visibility == Visibility.Visible)
        {
            this.Visibility = Visibility.Collapsed;
        }
        else
        {
            this.Visibility = Visibility.Visible;
            this.Activate();
            if (_currentLockMode == LockMode.SuperLocked)
            {
                EnforceTopmost();
            }
        }
    }

    private void EnforceTopmost()
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
        }
        catch { }
    }

    private void SetDockMode(DockMode mode, bool save = true)
    {
        _currentDockMode = mode;
        MenuModeFloating.IsChecked = mode == DockMode.Floating;
        MenuModeEmbedded.IsChecked = mode == DockMode.Embedded;

        if (mode == DockMode.Floating)
        {
            RootBorder.CornerRadius = new CornerRadius(16);
            RootBorder.Margin = new Thickness(4);
            RootShadow.Opacity = 0.45;
        }
        else
        {
            RootBorder.CornerRadius = new CornerRadius(10, 10, 0, 0);
            RootBorder.Margin = new Thickness(2, 2, 2, 0);
            RootShadow.Opacity = 0.20;
        }

        CompactBar.Height = 52;
        CompactArtBorder.Width = 38;
        CompactArtBorder.Height = 38;

        ApplyPosition(_currentPositionPreset);

        if (save)
        {
            SaveSettings();
        }
    }

    private void SetLockMode(LockMode mode, bool save = true)
    {
        _currentLockMode = mode;

        MenuLockFree.IsChecked = mode == LockMode.Free;
        MenuLockFixed.IsChecked = mode == LockMode.Locked;
        MenuLockSuper.IsChecked = mode == LockMode.SuperLocked;

        SymbolRegular symbol;
        string tip;
        Brush brush;

        switch (mode)
        {
            case LockMode.SuperLocked:
                symbol = SymbolRegular.Pin24;
                tip = "Süper Kilit (Oyunların ve Tüm Pencerelerin En Üstünde)";
                brush = new SolidColorBrush(Color.FromRgb(255, 59, 48));
                _superLockTimer.Start();
                EnforceTopmost();
                break;

            case LockMode.Locked:
                symbol = SymbolRegular.LockClosed24;
                tip = "Sabit Mod (Sürüklenemez)";
                brush = new SolidColorBrush(Colors.White);
                _superLockTimer.Stop();
                this.Topmost = true;
                break;

            case LockMode.Free:
            default:
                symbol = SymbolRegular.LockOpen24;
                tip = "Serbest Mod (Sürüklenebilir)";
                brush = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255));
                _superLockTimer.Stop();
                this.Topmost = true;
                break;
        }

        if (BtnLockCompact != null)
        {
            BtnLockCompact.Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = symbol };
            BtnLockCompact.ToolTip = tip;
            BtnLockCompact.Foreground = brush;
        }

        if (BtnLockExpanded != null)
        {
            BtnLockExpanded.Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = symbol };
            BtnLockExpanded.ToolTip = tip;
            BtnLockExpanded.Foreground = brush;
        }

        if (save)
        {
            SaveSettings();
        }
    }

    private void CycleLockMode()
    {
        switch (_currentLockMode)
        {
            case LockMode.Free:
                SetLockMode(LockMode.Locked);
                break;
            case LockMode.Locked:
                SetLockMode(LockMode.SuperLocked);
                break;
            case LockMode.SuperLocked:
            default:
                SetLockMode(LockMode.Free);
                break;
        }
    }

    private void BtnLock_Click(object sender, RoutedEventArgs e)
    {
        CycleLockMode();
    }

    private void MenuLockFree_Click(object sender, RoutedEventArgs e) => SetLockMode(LockMode.Free);
    private void MenuLockFixed_Click(object sender, RoutedEventArgs e) => SetLockMode(LockMode.Locked);
    private void MenuLockSuper_Click(object sender, RoutedEventArgs e) => SetLockMode(LockMode.SuperLocked);

    private void MenuModeFloating_Click(object sender, RoutedEventArgs e) => SetDockMode(DockMode.Floating);
    private void MenuModeEmbedded_Click(object sender, RoutedEventArgs e) => SetDockMode(DockMode.Embedded);

    private void ClampToWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        double h = this.Height > 0 ? this.Height : 68;
        double w = this.Width > 0 ? this.Width : 430;

        if (_currentDockMode == DockMode.Embedded && !_isExpanded && _currentPositionPreset != "custom")
        {
            this.Top = workArea.Bottom - h + 1;
        }
        else
        {
            double maxTop = workArea.Bottom - h - (_currentDockMode == DockMode.Embedded ? -1 : 10);
            if (this.Top > maxTop)
            {
                this.Top = maxTop;
            }
            if (this.Top < workArea.Top + 10)
            {
                this.Top = workArea.Top + 10;
            }
        }

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
            this.Width = 430;
            this.Height = 68;
            _isExpanded = false;

            var settings = SettingsManager.Load();
            _currentPositionPreset = string.IsNullOrEmpty(settings.PositionPreset) ? "left" : settings.PositionPreset;

            LockMode lockMode = settings.LockMode switch
            {
                "superlocked" => LockMode.SuperLocked,
                "locked" => LockMode.Locked,
                _ => LockMode.Free
            };
            SetLockMode(lockMode, save: false);

            _currentDockMode = settings.DockMode == "embedded" ? DockMode.Embedded : DockMode.Floating;
            SetDockMode(_currentDockMode, save: false);

            if (_currentPositionPreset == "custom" && settings.CustomLeft > 0 && settings.CustomTop > 0)
            {
                this.Left = settings.CustomLeft;
                this.Top = settings.CustomTop;
                ClampToWorkArea();
                return;
            }
        }
        catch { }

        ApplyPosition(_currentPositionPreset);
    }

    private void SaveSettings()
    {
        try
        {
            ClampToWorkArea();
            var settings = new AppSettings
            {
                PositionPreset = _currentPositionPreset,
                LockMode = _currentLockMode switch
                {
                    LockMode.SuperLocked => "superlocked",
                    LockMode.Locked => "locked",
                    _ => "free"
                },
                DockMode = _currentDockMode == DockMode.Embedded ? "embedded" : "floating",
                CustomLeft = this.Left,
                CustomTop = this.Top
            };
            SettingsManager.Save(settings);
        }
        catch { }
    }

    private void ApplyPosition(string position)
    {
        _currentPositionPreset = position;
        var workArea = SystemParameters.WorkArea;
        double w = 430;
        double compactHeight = 68;

        this.Width = w;
        if (!_isExpanded)
        {
            this.Height = compactHeight;
        }

        switch (position)
        {
            case "left":
                this.Left = workArea.Left + 16;
                break;
            case "center":
                this.Left = workArea.Left + (workArea.Width - w) / 2;
                break;
            case "right":
            default:
                this.Left = workArea.Right - w - 16;
                break;
        }

        if (_currentDockMode == DockMode.Embedded)
        {
            this.Top = workArea.Bottom - compactHeight + 1;
        }
        else
        {
            this.Top = workArea.Bottom - compactHeight - 10;
        }

        ClampToWorkArea();
        SaveSettings();
    }

    private void BtnExpand_Click(object sender, RoutedEventArgs e)
    {
        _isExpanded = !_isExpanded;
        var workArea = SystemParameters.WorkArea;
        double targetHeight = _isExpanded ? 245 : 68;

        // Keep bottom edge firmly anchored
        double currentBottom = this.Top + this.Height;
        if (currentBottom > workArea.Bottom + 20 || currentBottom < workArea.Top + 100)
        {
            currentBottom = _currentDockMode == DockMode.Embedded ? workArea.Bottom + 1 : workArea.Bottom - 10;
        }

        double targetTop = currentBottom - targetHeight;

        if (_isExpanded)
        {
            CompactBar.Visibility = Visibility.Collapsed;
            ExpandedView.Visibility = Visibility.Visible;
            SyncVolumeUI();
        }

        // Smooth animations (Slide & Fade)
        var duration = TimeSpan.FromMilliseconds(220);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        var animHeight = new DoubleAnimation(this.Height, targetHeight, duration) { EasingFunction = ease };
        var animTop = new DoubleAnimation(this.Top, targetTop, duration) { EasingFunction = ease };
        var animFade = new DoubleAnimation(_isExpanded ? 0 : 1, _isExpanded ? 1 : 0, duration) { EasingFunction = ease };

        animHeight.Completed += (s, ev) =>
        {
            if (!_isExpanded)
            {
                ExpandedView.Visibility = Visibility.Collapsed;
                CompactBar.Visibility = Visibility.Visible;
            }
            this.Height = targetHeight;
            this.Top = targetTop;
            ClampToWorkArea();
            if (_currentLockMode == LockMode.SuperLocked)
            {
                EnforceTopmost();
            }
        };

        ExpandedView.BeginAnimation(UIElement.OpacityProperty, animFade);
        this.BeginAnimation(Window.HeightProperty, animHeight);
        this.BeginAnimation(Window.TopProperty, animTop);
    }

    private void MenuChoosePos_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new PositionDialog(_currentPositionPreset, _currentDockMode == DockMode.Embedded ? "embedded" : "floating");
        if (dlg.ShowDialog() == true)
        {
            _currentDockMode = dlg.SelectedMode == "embedded" ? DockMode.Embedded : DockMode.Floating;
            SetDockMode(_currentDockMode, save: false);
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
        _trayManager.Dispose();
        Application.Current.Shutdown();
    }

    private void MediaController_OnMediaChanged(string title, string artist, BitmapImage? albumArt, MediaAppType appType, string appName, string accentColorHex)
    {
        try
        {
            _currentAppType = appType;

            SongTitleText.Text = title;
            ArtistNameText.Text = artist;
            ExpandedTitleText.Text = title;
            ExpandedArtistText.Text = artist;
            ExpandedAlbumText.Text = $"{appName} • Windows";
            ExpandedSourceText.Text = appName;

            _trayManager.UpdateTooltip($"PommeBar: {title} - {artist} ({appName})");

            try
            {
                var color = (Color)ColorConverter.ConvertFromString(accentColorHex);
                var brush = new SolidColorBrush(color);
                TrackProgressBar.Foreground = brush;
                ExpandedSourceIcon.Foreground = brush;
                this.Resources["AccentFillColorDefaultBrush"] = brush;
                this.Resources["ControlStrongFillColorDefaultBrush"] = brush;

                if (_currentLockMode == LockMode.SuperLocked && BtnLockCompact != null)
                {
                    BtnLockCompact.Foreground = brush;
                    if (BtnLockExpanded != null) BtnLockExpanded.Foreground = brush;
                }
            }
            catch { }

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
            var symbol = isPlaying ? SymbolRegular.Pause24 : SymbolRegular.Play24;

            BtnPlayPause.Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = symbol };
            if (BtnPlayPauseExpanded != null)
            {
                BtnPlayPauseExpanded.Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = symbol };
            }
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
        e.Handled = true;
        await _mediaController.SkipPreviousAsync();
    }

    private async void BtnPlayPause_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        await _mediaController.TogglePlayPauseAsync();
    }

    private async void BtnNext_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        await _mediaController.SkipNextAsync();
    }

    private async void BtnHeart_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        BtnHeart.IsEnabled = false;
        if (BtnHeartExpanded != null) BtnHeartExpanded.IsEnabled = false;
        try
        {
            if (_currentAppType == MediaAppType.Spotify)
            {
                await SpotifyAutomation.ToggleFavoriteAsync();
            }
            else
            {
                await AppleMusicAutomation.ToggleFavoriteAsync();
            }
        }
        finally
        {
            BtnHeart.IsEnabled = true;
            if (BtnHeartExpanded != null) BtnHeartExpanded.IsEnabled = true;
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_currentLockMode != LockMode.Free) return;

        if (e.ChangedButton == MouseButton.Left)
        {
            this.DragMove();
            _currentPositionPreset = "custom";
            ClampToWorkArea();
            SaveSettings();
        }
    }

    private void SongInfo_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        try
        {
            string procName = _currentAppType == MediaAppType.Spotify ? "Spotify" : "AppleMusic";
            var proc = System.Diagnostics.Process.GetProcessesByName(procName).FirstOrDefault();
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