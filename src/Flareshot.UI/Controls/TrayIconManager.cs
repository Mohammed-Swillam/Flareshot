using System.Drawing;
using System.Windows.Forms;

namespace Flareshot.UI.Controls;

/// <summary>
/// WPF-friendly wrapper for NotifyIcon (system tray icon).
/// </summary>
public class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;
    private bool _disposed;

    /// <summary>
    /// Event raised when the tray icon is double-clicked.
    /// </summary>
    public event EventHandler? DoubleClicked;

    /// <summary>
    /// Event raised when a balloon notification is clicked.
    /// </summary>
    public event EventHandler? BalloonClicked;

    /// <summary>
    /// Event raised when "New Screenshot" is clicked.
    /// </summary>
    public event EventHandler? CaptureClicked;

    /// <summary>
    /// Event raised when "Settings" is clicked.
    /// </summary>
    public event EventHandler? SettingsClicked;

    /// <summary>
    /// Event raised when "Exit" is clicked.
    /// </summary>
    public event EventHandler? ExitClicked;

    /// <summary>
    /// Gets or sets whether auto-start is enabled.
    /// </summary>
    public bool AutoStartEnabled
    {
        get => _autoStartMenuItem?.Checked ?? false;
        set
        {
            if (_autoStartMenuItem != null)
            {
                _autoStartMenuItem.Checked = value;
            }
        }
    }

    private ToolStripMenuItem? _autoStartMenuItem;

    /// <summary>
    /// Event raised when auto-start toggle is changed.
    /// </summary>
    public event EventHandler<bool>? AutoStartChanged;

    public TrayIconManager()
    {
        // Create context menu
        _contextMenu = new ContextMenuStrip();
        BuildContextMenu();

        // Create notify icon
        _notifyIcon = new NotifyIcon
        {
            Icon = CreateDefaultIcon(),
            Text = "Flareshot",
            Visible = false,
            ContextMenuStrip = _contextMenu
        };

        _notifyIcon.DoubleClick += (s, e) => DoubleClicked?.Invoke(this, EventArgs.Empty);
        _notifyIcon.BalloonTipClicked += (s, e) => BalloonClicked?.Invoke(this, EventArgs.Empty);
    }

    private void BuildContextMenu()
    {
        // New Screenshot
        var captureItem = new ToolStripMenuItem("📷 New Screenshot")
        {
            Font = new Font(_contextMenu.Font, FontStyle.Bold)
        };
        captureItem.Click += (s, e) => CaptureClicked?.Invoke(this, EventArgs.Empty);
        _contextMenu.Items.Add(captureItem);

        // Settings
        var settingsItem = new ToolStripMenuItem("⚙️ Settings");
        settingsItem.Click += (s, e) => SettingsClicked?.Invoke(this, EventArgs.Empty);
        _contextMenu.Items.Add(settingsItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // Auto-start
        _autoStartMenuItem = new ToolStripMenuItem("Start with Windows")
        {
            CheckOnClick = true
        };
        _autoStartMenuItem.CheckedChanged += (s, e) =>
        {
            AutoStartChanged?.Invoke(this, _autoStartMenuItem.Checked);
        };
        _contextMenu.Items.Add(_autoStartMenuItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // Exit
        var exitItem = new ToolStripMenuItem("❌ Exit");
        exitItem.Click += (s, e) => ExitClicked?.Invoke(this, EventArgs.Empty);
        _contextMenu.Items.Add(exitItem);

        // Style the menu
        _contextMenu.Renderer = new DarkMenuRenderer();
    }

    /// <summary>
    /// Shows the tray icon.
    /// </summary>
    public void Show()
    {
        _notifyIcon.Visible = true;
    }

    /// <summary>
    /// Hides the tray icon.
    /// </summary>
    public void Hide()
    {
        _notifyIcon.Visible = false;
    }

    /// <summary>
    /// Shows a balloon notification.
    /// </summary>
    public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info, int timeout = 3000)
    {
        _notifyIcon.ShowBalloonTip(timeout, title, message, icon);
    }

    /// <summary>
    /// Sets the tray icon.
    /// </summary>
    public void SetIcon(Icon icon)
    {
        _notifyIcon.Icon = icon;
    }

    /// <summary>
    /// Creates a simple default icon (blue square with white camera).
    /// </summary>
    private static Icon CreateDefaultIcon()
    {
        // Create a simple 16x16 icon programmatically
        using var bitmap = new Bitmap(16, 16);
        using var graphics = Graphics.FromImage(bitmap);

        // Blue background
        using var brush = new SolidBrush(Color.FromArgb(33, 150, 243));
        graphics.FillRectangle(brush, 0, 0, 16, 16);

        // White camera body (simplified)
        using var whiteBrush = new SolidBrush(Color.White);
        graphics.FillRectangle(whiteBrush, 3, 5, 10, 7);
        graphics.FillRectangle(whiteBrush, 5, 3, 4, 2);

        // Camera lens (circle)
        using var lensBrush = new SolidBrush(Color.FromArgb(33, 150, 243));
        graphics.FillEllipse(lensBrush, 5, 6, 5, 5);

        // Convert to icon
        IntPtr hIcon = bitmap.GetHicon();
        return Icon.FromHandle(hIcon);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Dark theme renderer for the context menu.
/// </summary>
internal class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    public DarkMenuRenderer() : base(new DarkColorTable()) { }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = Color.White;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item.Selected)
        {
            using var brush = new SolidBrush(Color.FromArgb(60, 60, 60));
            e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
        }
        else
        {
            using var brush = new SolidBrush(Color.FromArgb(30, 30, 30));
            e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
        }
    }
}

/// <summary>
/// Dark color table for the menu renderer.
/// </summary>
internal class DarkColorTable : ProfessionalColorTable
{
    public override Color MenuBorder => Color.FromArgb(60, 60, 60);
    public override Color MenuItemBorder => Color.FromArgb(60, 60, 60);
    public override Color MenuItemSelected => Color.FromArgb(60, 60, 60);
    public override Color MenuStripGradientBegin => Color.FromArgb(30, 30, 30);
    public override Color MenuStripGradientEnd => Color.FromArgb(30, 30, 30);
    public override Color MenuItemSelectedGradientBegin => Color.FromArgb(60, 60, 60);
    public override Color MenuItemSelectedGradientEnd => Color.FromArgb(60, 60, 60);
    public override Color MenuItemPressedGradientBegin => Color.FromArgb(45, 45, 45);
    public override Color MenuItemPressedGradientEnd => Color.FromArgb(45, 45, 45);
    public override Color ToolStripDropDownBackground => Color.FromArgb(30, 30, 30);
    public override Color ImageMarginGradientBegin => Color.FromArgb(30, 30, 30);
    public override Color ImageMarginGradientMiddle => Color.FromArgb(30, 30, 30);
    public override Color ImageMarginGradientEnd => Color.FromArgb(30, 30, 30);
    public override Color SeparatorDark => Color.FromArgb(60, 60, 60);
    public override Color SeparatorLight => Color.FromArgb(60, 60, 60);
}
