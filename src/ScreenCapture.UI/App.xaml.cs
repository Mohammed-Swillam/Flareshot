using System.Windows;
using ScreenCapture.Core.Services;

using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace ScreenCapture.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static Mutex? _mutex;
    private const string MutexName = "ScreenCapture.NET.SingleInstance";

    private ISettingsManager? _settingsManager;

    /// <summary>
    /// Gets the settings manager instance.
    /// </summary>
    public ISettingsManager SettingsManager => _settingsManager 
        ?? throw new InvalidOperationException("Settings manager not initialized.");

    /// <summary>
    /// Gets whether the application was started with --minimized argument.
    /// </summary>
    public bool StartMinimized { get; private set; }

    /// <summary>
    /// Application entry point with single-instance enforcement.
    /// </summary>
    [STAThread]
    public static void Main(string[] args)
    {
        // Check for single instance
        _mutex = new Mutex(true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            // Another instance is already running
            MessageBox.Show(
                "ScreenCapture.NET is already running.\nCheck your system tray.",
                "Already Running",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        try
        {
            var app = new App();
            app.InitializeComponent();
            app.ParseArguments(args);
            app.Run();
        }
        finally
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
    }

    /// <summary>
    /// Parse command line arguments.
    /// </summary>
    private void ParseArguments(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg.Equals("--minimized", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-m", StringComparison.OrdinalIgnoreCase))
            {
                StartMinimized = true;
            }
        }
    }

    /// <summary>
    /// Application startup handler.
    /// </summary>
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize settings manager
        _settingsManager = new SettingsManager();
        var settings = await _settingsManager.LoadAsync();

        // Check if we should start minimized (from args or settings)
        var shouldStartMinimized = StartMinimized || settings.StartMinimized;

        // Create and show main window
        var mainWindow = new MainWindow();
        MainWindow = mainWindow;

        if (shouldStartMinimized)
        {
            // Start minimized to tray (window won't be shown)
            mainWindow.WindowState = WindowState.Minimized;
            mainWindow.ShowInTaskbar = false;
            // Don't show the window - it will appear in system tray
        }
        else
        {
            mainWindow.Show();
        }
    }

    /// <summary>
    /// Application exit handler.
    /// </summary>
    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }
}
