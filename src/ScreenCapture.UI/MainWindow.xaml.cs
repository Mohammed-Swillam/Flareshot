using System.ComponentModel;
using System.Windows;
using ScreenCapture.UI.Views;

using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace ScreenCapture.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handle closing - minimize to tray instead of closing.
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        // Minimize to tray instead of closing
        e.Cancel = true;
        WindowState = WindowState.Minimized;
        ShowInTaskbar = false;
        Hide();

        base.OnClosing(e);
    }

    /// <summary>
    /// Handle state changes to update taskbar visibility.
    /// </summary>
    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            ShowInTaskbar = false;
            Hide();
        }
        else
        {
            ShowInTaskbar = true;
        }

        base.OnStateChanged(e);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var app = (App)Application.Current;
        var settingsWindow = new SettingsWindow(app.SettingsManager)
        {
            Owner = this
        };
        settingsWindow.ShowDialog();
    }

    private void CaptureButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement capture trigger
        MessageBox.Show(
            "Screen capture will be implemented in the next section!",
            "Coming Soon",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}