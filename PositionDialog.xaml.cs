using System.Windows;
using System.Windows.Media;

namespace PommeBar;

public partial class PositionDialog : Window
{
    public string SelectedPosition { get; set; } = "left";
    public string SelectedMode { get; set; } = "floating";

    public PositionDialog(string currentPos = "left", string currentMode = "floating")
    {
        InitializeComponent();
        SelectedPosition = currentPos;
        SelectedMode = currentMode;
        UpdateModeHighlights();
    }

    private void UpdateModeHighlights()
    {
        var accentBrush = new SolidColorBrush(Color.FromRgb(255, 59, 48));
        var defaultBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));

        BtnModeFloating.BorderBrush = SelectedMode == "floating" ? accentBrush : defaultBrush;
        BtnModeEmbedded.BorderBrush = SelectedMode == "embedded" ? accentBrush : defaultBrush;
    }

    private void BtnModeFloating_Click(object sender, RoutedEventArgs e)
    {
        SelectedMode = "floating";
        UpdateModeHighlights();
    }

    private void BtnModeEmbedded_Click(object sender, RoutedEventArgs e)
    {
        SelectedMode = "embedded";
        UpdateModeHighlights();
    }

    private void BtnLeft_Click(object sender, RoutedEventArgs e)
    {
        SelectedPosition = "left";
        DialogResult = true;
        Close();
    }

    private void BtnCenter_Click(object sender, RoutedEventArgs e)
    {
        SelectedPosition = "center";
        DialogResult = true;
        Close();
    }

    private void BtnRight_Click(object sender, RoutedEventArgs e)
    {
        SelectedPosition = "right";
        DialogResult = true;
        Close();
    }
}
