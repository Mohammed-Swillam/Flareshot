using System.Windows;
using System.Windows.Interop;
using ScreenCapture.Core.Capture;
using ScreenCapture.Core.Hotkeys;
using ScreenCapture.Core.Models;
using ScreenCapture.Core.Services;
using ScreenCapture.UI.Controls;
using ScreenCapture.UI.Views;

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
    private const int CaptureHotkeyId = 1;

    private ISettingsManager? _settingsManager;
    private IAutoStartManager? _autoStartManager;
    private IGlobalHotkeyManager? _hotkeyManager;
    private IScreenCaptureService? _captureService;
    private TrayIconManager? _trayIcon;
    private HwndSource? _hwndSource;
    private OverlayWindow? _overlayWindow;

    /// <summary>
    /// Gets the settings manager instance.
    /// </summary>
    public ISettingsManager SettingsManager => _settingsManager 
        ?? throw new InvalidOperationException("Settings manager not initialized.");

    /// <summary>
    /// Gets the auto-start manager instance.
    /// </summary>
    public IAutoStartManager AutoStartManager => _autoStartManager
        ?? throw new InvalidOperationException("Auto-start manager not initialized.");

    /// <summary>
    /// Gets whether the application was started with --minimized argument.
    /// </summary>
    public bool StartMinimized { get; private set; }

    /// <summary>
    /// Public method to trigger capture from MainWindow.
    /// </summary>
    public void TriggerCapturePublic() => TriggerCapture();

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

        // Initialize managers
        _settingsManager = new SettingsManager();
        _autoStartManager = new AutoStartManager();
        var settings = await _settingsManager.LoadAsync();

        // Subscribe to settings changes
        _settingsManager.SettingsChanged += OnSettingsChanged;

        // Check if we should start minimized (from args or settings)
        var shouldStartMinimized = StartMinimized || settings.StartMinimized;

        // Create and show main window
        var mainWindow = new MainWindow();
        MainWindow = mainWindow;

        // Initialize system tray
        InitializeTrayIcon(settings);

        // Show window or minimize to tray
        if (shouldStartMinimized)
        {
            // Start minimized to tray (window won't be shown)
            mainWindow.WindowState = WindowState.Minimized;
            mainWindow.ShowInTaskbar = false;
            
            // Show notification that app is running in tray
            _trayIcon?.ShowNotification(
                "ScreenCapture.NET",
                $"Running in system tray. Press {GlobalHotkeyManager.GetHotkeyDisplayString(settings.HotkeyKey, settings.HotkeyModifiers)} to capture.",
                System.Windows.Forms.ToolTipIcon.Info);
        }
        else
        {
            mainWindow.Show();
        }

        // Initialize hotkey after window is created (needs window handle)
        mainWindow.SourceInitialized += (s, args) =>
        {
            InitializeHotkey(mainWindow, settings);
        };

        // If window is already initialized, set up hotkey immediately
        if (PresentationSource.FromVisual(mainWindow) != null)
        {
            InitializeHotkey(mainWindow, settings);
        }
    }

    /// <summary>
    /// Initialize the system tray icon.
    /// </summary>
    private void InitializeTrayIcon(AppSettings settings)
    {
        _trayIcon = new TrayIconManager();
        _trayIcon.AutoStartEnabled = _autoStartManager!.IsEnabled;

        _trayIcon.DoubleClicked += (s, e) => ShowMainWindow();
        _trayIcon.CaptureClicked += (s, e) => TriggerCapture();
        _trayIcon.SettingsClicked += (s, e) => ShowSettings();
        _trayIcon.ExitClicked += (s, e) => ExitApplication();
        _trayIcon.AutoStartChanged += OnAutoStartChanged;

        _trayIcon.Show();
    }

    /// <summary>
    /// Initialize the global hotkey.
    /// </summary>
    private void InitializeHotkey(Window window, AppSettings settings)
    {
        var helper = new WindowInteropHelper(window);
        var hwnd = helper.Handle;

        if (hwnd == IntPtr.Zero) return;

        // Create HWND source for message handling
        _hwndSource = HwndSource.FromHwnd(hwnd);
        _hwndSource?.AddHook(WndProc);

        // Create hotkey manager
        _hotkeyManager = new GlobalHotkeyManager(hwnd);
        _hotkeyManager.HotkeyPressed += OnHotkeyPressed;

        // Register the capture hotkey
        RegisterCaptureHotkey(settings);
    }

    /// <summary>
    /// Register the capture hotkey based on settings.
    /// </summary>
    private void RegisterCaptureHotkey(AppSettings settings)
    {
        if (_hotkeyManager == null) return;

        bool success = _hotkeyManager.RegisterHotkey(
            CaptureHotkeyId,
            settings.HotkeyKey,
            settings.HotkeyModifiers);

        if (!success)
        {
            var hotkeyDisplay = GlobalHotkeyManager.GetHotkeyDisplayString(
                settings.HotkeyKey, settings.HotkeyModifiers);

            _trayIcon?.ShowNotification(
                "Hotkey Registration Failed",
                $"Could not register {hotkeyDisplay}. It may be in use by another application.",
                System.Windows.Forms.ToolTipIcon.Warning);
        }
    }

    /// <summary>
    /// Window message handler for processing hotkey messages.
    /// </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (_hotkeyManager?.ProcessMessage(msg, wParam, lParam) == true)
        {
            handled = true;
        }
        return IntPtr.Zero;
    }

    /// <summary>
    /// Handle hotkey pressed event.
    /// </summary>
    private void OnHotkeyPressed(object? sender, HotkeyEventArgs e)
    {
        if (e.HotkeyId == CaptureHotkeyId)
        {
            TriggerCapture();
        }
    }

    /// <summary>
    /// Handle settings changed event.
    /// </summary>
    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        // Re-register hotkey if it changed
        if (e.OldSettings.HotkeyKey != e.NewSettings.HotkeyKey ||
            e.OldSettings.HotkeyModifiers != e.NewSettings.HotkeyModifiers)
        {
            _hotkeyManager?.UnregisterHotkey(CaptureHotkeyId);
            RegisterCaptureHotkey(e.NewSettings);
        }

        // Update auto-start if it changed
        if (e.OldSettings.AutoStartWithWindows != e.NewSettings.AutoStartWithWindows)
        {
            UpdateAutoStart(e.NewSettings.AutoStartWithWindows);
        }
    }

    /// <summary>
    /// Handle auto-start toggle from tray menu.
    /// </summary>
    private async void OnAutoStartChanged(object? sender, bool enabled)
    {
        UpdateAutoStart(enabled);

        // Update settings
        var settings = _settingsManager!.CurrentSettings;
        settings.AutoStartWithWindows = enabled;
        await _settingsManager.SaveAsync(settings);
    }

    /// <summary>
    /// Update auto-start registry.
    /// </summary>
    private void UpdateAutoStart(bool enabled)
    {
        var exePath = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;

        if (enabled)
        {
            _autoStartManager?.Enable(exePath, startMinimized: true);
        }
        else
        {
            _autoStartManager?.Disable();
        }

        if (_trayIcon != null)
        {
            _trayIcon.AutoStartEnabled = enabled;
        }
    }

    /// <summary>
    /// Trigger screen capture.
    /// </summary>
    private void TriggerCapture()
    {
        // Don't open another overlay if one is already open
        if (_overlayWindow != null)
        {
            return;
        }

        // Initialize capture service if needed
        _captureService ??= new ScreenCaptureService();

        // Hide main window during capture
        MainWindow?.Hide();

        // Small delay to ensure window is hidden
        System.Threading.Thread.Sleep(100);

        // Create and show overlay
        _overlayWindow = new OverlayWindow(_captureService);
        _overlayWindow.SelectionConfirmed += OnSelectionConfirmed;
        _overlayWindow.SelectionCancelled += OnSelectionCancelled;
        _overlayWindow.Closed += OnOverlayClosed;
        _overlayWindow.Show();
    }

    /// <summary>
    /// Handle selection confirmed.
    /// </summary>
    private void OnSelectionConfirmed(object? sender, SelectionConfirmedEventArgs e)
    {
        // For now, just show a notification with the selected region
        // Full annotation support will be added in later sections
        _trayIcon?.ShowNotification(
            "Selection Captured",
            $"Selected region: {e.SelectedRegion.Width} × {e.SelectedRegion.Height}",
            System.Windows.Forms.ToolTipIcon.Info);

        // Close the overlay
        _overlayWindow?.Close();
    }

    /// <summary>
    /// Handle selection cancelled.
    /// </summary>
    private void OnSelectionCancelled(object? sender, EventArgs e)
    {
        // Overlay closes itself
    }

    /// <summary>
    /// Handle overlay closed.
    /// </summary>
    private void OnOverlayClosed(object? sender, EventArgs e)
    {
        if (_overlayWindow != null)
        {
            _overlayWindow.SelectionConfirmed -= OnSelectionConfirmed;
            _overlayWindow.SelectionCancelled -= OnSelectionCancelled;
            _overlayWindow.Closed -= OnOverlayClosed;
            _overlayWindow = null;
        }
    }

    /// <summary>
    /// Show the main window.
    /// </summary>
    private void ShowMainWindow()
    {
        if (MainWindow == null) return;

        MainWindow.Show();
        MainWindow.WindowState = WindowState.Normal;
        MainWindow.ShowInTaskbar = true;
        MainWindow.Activate();
    }

    /// <summary>
    /// Show the settings window.
    /// </summary>
    private void ShowSettings()
    {
        var settingsWindow = new SettingsWindow(_settingsManager!)
        {
            Owner = MainWindow?.IsVisible == true ? MainWindow : null
        };
        settingsWindow.ShowDialog();
    }

    /// <summary>
    /// Exit the application.
    /// </summary>
    private void ExitApplication()
    {
        _trayIcon?.Hide();
        Shutdown();
    }

    /// <summary>
    /// Application exit handler.
    /// </summary>
    protected override void OnExit(ExitEventArgs e)
    {
        // Cleanup
        _hotkeyManager?.Dispose();
        _trayIcon?.Dispose();
        _hwndSource?.RemoveHook(WndProc);

        if (_settingsManager != null)
        {
            _settingsManager.SettingsChanged -= OnSettingsChanged;
        }

        base.OnExit(e);
    }
}
