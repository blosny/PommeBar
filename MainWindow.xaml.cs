using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

namespace PommeBar;

public partial class MainWindow : FluentWindow
{
    private readonly MediaController _mediaController;

    public MainWindow()
    {
        InitializeComponent();
        Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);

        _mediaController = new MediaController();
        _mediaController.OnMediaChanged += MediaController_OnMediaChanged;
        _mediaController.OnPlaybackStateChanged += MediaController_OnPlaybackStateChanged;
        
        this.Loaded += async (s, e) =>
        {
            MenuStartup.IsChecked = StartupManager.IsStartupEnabled();
            await _mediaController.InitializeAsync();
        };
    }

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
        SongTitleText.Text = title;
        ArtistNameText.Text = artist;

        if (albumArt != null)
        {
            AlbumArtImage.Source = albumArt;
            AlbumArtImage.Visibility = Visibility.Visible;
        }
        else
        {
            AlbumArtImage.Source = null;
            AlbumArtImage.Visibility = Visibility.Collapsed;
        }
    }

    private void MediaController_OnPlaybackStateChanged(bool isPlaying)
    {
        BtnPlayPause.Icon = new Wpf.Ui.Controls.SymbolIcon
        {
            Symbol = isPlaying ? Wpf.Ui.Controls.SymbolRegular.Pause24 : Wpf.Ui.Controls.SymbolRegular.Play24
        };
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
}