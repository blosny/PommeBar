using System.Windows;

namespace PommeBar;

public partial class PositionDialog : Window
{
    public string SelectedPosition { get; private set; } = "right";

    public PositionDialog()
    {
        InitializeComponent();
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
