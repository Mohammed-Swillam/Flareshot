using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using ScreenCapture.Core.Capture;
using ScreenCapture.Core.Hotkeys;
using ScreenCapture.Core.IO;
using ScreenCapture.Core.Logging;
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
    private IClipboardService? _clipboardService;
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
    /// Gets whether debug logging is enabled (via --debug argument).
    /// </summary>
    public bool DebugLoggingEnabled { get; private set; }

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
            else if (arg.Equals("--debug", StringComparison.OrdinalIgnoreCase) ||
                     arg.Equals("--log", StringComparison.OrdinalIgnoreCase))
            {
                DebugLoggingEnabled = true;
            }
        }
    }

    /// <summary>
    /// Application startup handler.
    /// </summary>
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize logger only if --debug flag is set
        if (DebugLoggingEnabled)
        {
            FileLogger.Instance.Info("Application starting (debug mode)", "App");
            FileLogger.Instance.CleanupOldLogs(7);
        }

        // Initialize managers
        _settingsManager = new SettingsManager();
        _autoStartManager = new AutoStartManager();
        var settings = await _settingsManager.LoadAsync();

        if (DebugLoggingEnabled)
        {
            FileLogger.Instance.Info("Settings loaded", "App");
        }

        // Subscribe to settings changes
        _settingsManager.SettingsChanged += OnSettingsChanged;

        // Check if we should start minimized (from args or settings)
        var shouldStartMinimized = StartMinimized || settings.StartMinimized;

        // Create and show main window
        var mainWindow = new MainWindow();
        MainWindow = mainWindow;

        // Initialize system tray
        InitializeTrayIcon(settings);

        // We need to show the window briefly to create the window handle for hotkey registration
        // Then we can hide it if starting minimized
        mainWindow.Show();
        
        // Force window handle creation by getting the helper
        var helper = new WindowInteropHelper(mainWindow);
        helper.EnsureHandle();

        // Initialize hotkey immediately since we now have a handle
        InitializeHotkey(mainWindow, settings);

        // Now minimize to tray if needed
        if (shouldStartMinimized)
        {
            mainWindow.Hide();
            mainWindow.WindowState = WindowState.Minimized;
            mainWindow.ShowInTaskbar = false;
            
            // Show notification that app is running in tray
            _trayIcon?.ShowNotification(
                "ScreenCapture.NET",
                $"Running in system tray. Press {GlobalHotkeyManager.GetHotkeyDisplayString(settings.HotkeyKey, settings.HotkeyModifiers)} to capture.",
                System.Windows.Forms.ToolTipIcon.Info);
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
        _trayIcon.BalloonClicked += OnBalloonClicked;

        _trayIcon.Show();
    }

    /// <summary>
    /// Handle balloon notification clicked.
    /// </summary>
    private void OnBalloonClicked(object? sender, EventArgs e)
    {
        // If we have a last saved file, open its folder
        if (!string.IsNullOrEmpty(_lastSavedFilePath) && System.IO.File.Exists(_lastSavedFilePath))
        {
            try
            {
                // Open explorer and select the file
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{_lastSavedFilePath}\"");
            }
            catch
            {
                // If that fails, try just opening the folder
                var folder = System.IO.Path.GetDirectoryName(_lastSavedFilePath);
                if (!string.IsNullOrEmpty(folder))
                {
                    System.Diagnostics.Process.Start("explorer.exe", folder);
                }
            }
        }
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
        _overlayWindow.FileSaved += OnFileSaved;
        _overlayWindow.Closed += OnOverlayClosed;
        _overlayWindow.Show();
    }

    /// <summary>
    /// Handle selection confirmed.
    /// </summary>
    private void OnSelectionConfirmed(object? sender, SelectionConfirmedEventArgs e)
    {
        // Initialize clipboard service if needed
        _clipboardService ??= new ClipboardService();

        // Crop the screenshot to the selected region
        var croppedBitmap = CropBitmap(e.Screenshot, e.SelectedRegion);
        
        if (croppedBitmap == null) return;

        // Composite annotations on top of screenshot if there are any
        BitmapSource finalBitmap = croppedBitmap;
        
        if (e.AnnotationCanvas != null && e.AnnotationCanvas.HasAnnotations)
        {
            finalBitmap = CompositeAnnotations(croppedBitmap, e.AnnotationCanvas, e.SelectedRegion);
        }

        // Copy to clipboard
        bool success = _clipboardService.CopyToClipboard(finalBitmap);

        if (success)
        {
            _trayIcon?.ShowNotification(
                "Copied to Clipboard",
                $"Screenshot ({e.SelectedRegion.Width} × {e.SelectedRegion.Height}) copied to clipboard.",
                System.Windows.Forms.ToolTipIcon.Info);
        }
        else
        {
            _trayIcon?.ShowNotification(
                "Clipboard Error",
                "Failed to copy to clipboard. It may be locked by another application.",
                System.Windows.Forms.ToolTipIcon.Warning);
        }

        // Close the overlay
        _overlayWindow?.Close();
    }

    /// <summary>
    /// Composite annotations on top of the screenshot.
    /// </summary>
    private static BitmapSource CompositeAnnotations(BitmapSource screenshot, Controls.DrawingCanvas canvas, System.Drawing.Rectangle region)
    {
        try
        {
            var visual = new System.Windows.Media.DrawingVisual();

            using (var context = visual.RenderOpen())
            {
                // Draw the screenshot
                context.DrawImage(screenshot, new Rect(0, 0, screenshot.PixelWidth, screenshot.PixelHeight));

                // Apply translation to render annotations at correct positions
                context.PushTransform(new System.Windows.Media.TranslateTransform(-region.X, -region.Y));

                // Draw each annotation
                foreach (var annotation in canvas.GetAll())
                {
                    annotation.Render(context);
                }

                context.Pop();
            }

            var renderBitmap = new RenderTargetBitmap(
                screenshot.PixelWidth,
                screenshot.PixelHeight,
                96, 96,
                System.Windows.Media.PixelFormats.Pbgra32);

            renderBitmap.Render(visual);
            renderBitmap.Freeze();

            return renderBitmap;
        }
        catch
        {
            // If compositing fails, return the original screenshot
            return screenshot;
        }
    }

    /// <summary>
    /// Crop a BitmapSource to the specified region.
    /// </summary>
    private static BitmapSource? CropBitmap(BitmapSource? source, System.Drawing.Rectangle region)
    {
        if (source == null) return null;

        try
        {
            // Calculate the crop region relative to the source
            // The overlay coordinates are already in screen space from Left/Top
            var cropRect = new Int32Rect(
                Math.Max(0, region.X),
                Math.Max(0, region.Y),
                Math.Min(region.Width, (int)source.PixelWidth - region.X),
                Math.Min(region.Height, (int)source.PixelHeight - region.Y));

            // Ensure valid dimensions
            if (cropRect.Width <= 0 || cropRect.Height <= 0)
                return null;

            var croppedBitmap = new CroppedBitmap(source, cropRect);
            croppedBitmap.Freeze();
            return croppedBitmap;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Handle selection cancelled.
    /// </summary>
    private void OnSelectionCancelled(object? sender, EventArgs e)
    {
        // Overlay closes itself
    }

    /// <summary>
    /// Last saved file path for notification click.
    /// </summary>
    private string? _lastSavedFilePath;

    /// <summary>
    /// Handle file saved.
    /// </summary>
    private void OnFileSaved(object? sender, FileSavedEventArgs e)
    {
        _lastSavedFilePath = e.FilePath;
        
        _trayIcon?.ShowNotification(
            "Screenshot Saved",
            $"Saved to: {System.IO.Path.GetFileName(e.FilePath)}\nClick to open folder.",
            System.Windows.Forms.ToolTipIcon.Info);
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
            _overlayWindow.FileSaved -= OnFileSaved;
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
        if (DebugLoggingEnabled)
        {
            FileLogger.Instance.Info("Application shutting down", "App");
        }

        // Cleanup
        _hotkeyManager?.Dispose();
        _trayIcon?.Dispose();
        _hwndSource?.RemoveHook(WndProc);

        if (_settingsManager != null)
        {
            _settingsManager.SettingsChanged -= OnSettingsChanged;
        }

        if (DebugLoggingEnabled)
        {
            FileLogger.Instance.Info("Application exited cleanly", "App");
            FileLogger.Instance.Dispose();
        }

        base.OnExit(e);
    }
}
