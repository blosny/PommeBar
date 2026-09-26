using System.Windows;
using System.Windows.Media;

namespace PommeBar;

public partial class PositionDialog : Window
{
    public string SelectedPosition { get; set; } = "left";
    public string SelectedMode { get; set; } = "floating";
    public string SelectedSize { get; set; } = "standard";
    public string SelectedFilter { get; set; } = "musiconly";

    private readonly SolidColorBrush _accentBrush = new SolidColorBrush(Color.FromRgb(255, 59, 48));
    private readonly SolidColorBrush _selectedBgBrush = new SolidColorBrush(Color.FromArgb(50, 255, 59, 48));
    private readonly SolidColorBrush _defaultBorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
    private readonly SolidColorBrush _transparentBrush = new SolidColorBrush(Colors.Transparent);

    public PositionDialog(string currentPos = "left", string currentMode = "floating", string currentSize = "standard", string currentFilter = "musiconly")
    {
        InitializeComponent();
        SelectedPosition = string.IsNullOrEmpty(currentPos) || currentPos == "custom" ? "left" : currentPos;
        SelectedMode = string.IsNullOrEmpty(currentMode) ? "floating" : currentMode;
        SelectedSize = string.IsNullOrEmpty(currentSize) ? "standard" : currentSize;
        SelectedFilter = string.IsNullOrEmpty(currentFilter) ? "musiconly" : currentFilter;

        UpdateHighlights();
    }

    private void UpdateHighlights()
    {
        // 1. Mode highlight
        bool isFloating = SelectedMode == "floating";
        BtnModeFloating.BorderBrush = isFloating ? _accentBrush : _defaultBorderBrush;
        BtnModeFloating.Background = isFloating ? _selectedBgBrush : _transparentBrush;

        BtnModeEmbedded.BorderBrush = !isFloating ? _accentBrush : _defaultBorderBrush;
        BtnModeEmbedded.Background = !isFloating ? _selectedBgBrush : _transparentBrush;

        // 2. Size highlight
        BtnSizeCompact.BorderBrush = SelectedSize == "compact" ? _accentBrush : _defaultBorderBrush;
        BtnSizeCompact.Background = SelectedSize == "compact" ? _selectedBgBrush : _transparentBrush;

        BtnSizeStandard.BorderBrush = SelectedSize == "standard" ? _accentBrush : _defaultBorderBrush;
        BtnSizeStandard.Background = SelectedSize == "standard" ? _selectedBgBrush : _transparentBrush;

        BtnSizeWide.BorderBrush = SelectedSize == "wide" ? _accentBrush : _defaultBorderBrush;
        BtnSizeWide.Background = SelectedSize == "wide" ? _selectedBgBrush : _transparentBrush;

        // 3. Position highlight
        BtnLeft.BorderBrush = SelectedPosition == "left" ? _accentBrush : _defaultBorderBrush;
        BtnLeft.Background = SelectedPosition == "left" ? _selectedBgBrush : _transparentBrush;

        BtnCenter.BorderBrush = SelectedPosition == "center" ? _accentBrush : _defaultBorderBrush;
        BtnCenter.Background = SelectedPosition == "center" ? _selectedBgBrush : _transparentBrush;

        BtnRight.BorderBrush = SelectedPosition == "right" ? _accentBrush : _defaultBorderBrush;
        BtnRight.Background = SelectedPosition == "right" ? _selectedBgBrush : _transparentBrush;

        // 4. Media filter highlight
        bool isMusicOnly = SelectedFilter == "musiconly";
        BtnFilterMusic.BorderBrush = isMusicOnly ? _accentBrush : _defaultBorderBrush;
        BtnFilterMusic.Background = isMusicOnly ? _selectedBgBrush : _transparentBrush;

        BtnFilterAll.BorderBrush = !isMusicOnly ? _accentBrush : _defaultBorderBrush;
        BtnFilterAll.Background = !isMusicOnly ? _selectedBgBrush : _transparentBrush;
    }

    private void BtnModeFloating_Click(object sender, RoutedEventArgs e)
    {
        SelectedMode = "floating";
        UpdateHighlights();
    }

    private void BtnModeEmbedded_Click(object sender, RoutedEventArgs e)
    {
        SelectedMode = "embedded";
        UpdateHighlights();
    }

    private void BtnSizeCompact_Click(object sender, RoutedEventArgs e)
    {
        SelectedSize = "compact";
        UpdateHighlights();
    }

    private void BtnSizeStandard_Click(object sender, RoutedEventArgs e)
    {
        SelectedSize = "standard";
        UpdateHighlights();
    }

    private void BtnSizeWide_Click(object sender, RoutedEventArgs e)
    {
        SelectedSize = "wide";
        UpdateHighlights();
    }

    private void BtnLeft_Click(object sender, RoutedEventArgs e)
    {
        SelectedPosition = "left";
        UpdateHighlights();
    }

    private void BtnCenter_Click(object sender, RoutedEventArgs e)
    {
        SelectedPosition = "center";
        UpdateHighlights();
    }

    private void BtnRight_Click(object sender, RoutedEventArgs e)
    {
        SelectedPosition = "right";
        UpdateHighlights();
    }

    private void BtnFilterMusic_Click(object sender, RoutedEventArgs e)
    {
        SelectedFilter = "musiconly";
        UpdateHighlights();
    }

    private void BtnFilterAll_Click(object sender, RoutedEventArgs e)
    {
        SelectedFilter = "allmedia";
        UpdateHighlights();
    }

    private void BtnApply_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
