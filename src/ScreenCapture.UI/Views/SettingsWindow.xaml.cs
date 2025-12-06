using System.Windows;
using System.Windows.Input;
using ScreenCapture.Core.Services;
using ScreenCapture.UI.ViewModels;

using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace ScreenCapture.UI.Views;

/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(ISettingsManager settingsManager)
    {
        InitializeComponent();

        _viewModel = new SettingsViewModel(settingsManager);
        DataContext = _viewModel;

        // Handle key recording
        PreviewKeyDown += SettingsWindow_PreviewKeyDown;
        
        // Handle save button to close window
        SaveButton.Click += (s, e) => Close();
        CancelButton.Click += (s, e) => Close();
    }

    private void SettingsWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_viewModel.IsRecordingHotkey)
        {
            e.Handled = true;
            _viewModel.RecordHotkey(e.Key);
        }
    }
}
