using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace Flareshot.Core.Capture;

/// <summary>
/// Represents information about a window.
/// </summary>
public class WindowInfo
{
    /// <summary>
    /// The window handle.
    /// </summary>
    public IntPtr Handle { get; set; }

    /// <summary>
    /// The window title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The window bounds in screen coordinates.
    /// </summary>
    public Rectangle Bounds { get; set; }

    /// <summary>
    /// The process name that owns this window.
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the window is visible.
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// Whether the window is minimized.
    /// </summary>
    public bool IsMinimized { get; set; }
}

/// <summary>
/// Interface for window detection operations.
/// </summary>
public interface IWindowDetectionService
{
    /// <summary>
    /// Gets all visible windows.
    /// </summary>
    IReadOnlyList<WindowInfo> GetVisibleWindows();

    /// <summary>
    /// Gets the window at the specified screen point.
    /// </summary>
    WindowInfo? GetWindowAtPoint(Point point);

    /// <summary>
    /// Gets the window bounds for a given handle.
    /// </summary>
    Rectangle GetWindowBounds(IntPtr hwnd);
}

/// <summary>
/// Service for detecting and enumerating windows.
/// </summary>
public class WindowDetectionService : IWindowDetectionService
{
    #region Native Methods

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const int WS_VISIBLE = 0x10000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_APPWINDOW = 0x00040000;
    private const uint GA_ROOT = 2;
    private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public Rectangle ToRectangle() => new(Left, Top, Right - Left, Bottom - Top);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    #endregion

    /// <inheritdoc />
    public IReadOnlyList<WindowInfo> GetVisibleWindows()
    {
        var windows = new List<WindowInfo>();

        EnumWindows((hwnd, lParam) =>
        {
            if (IsValidWindow(hwnd))
            {
                var info = CreateWindowInfo(hwnd);
                if (info != null && !string.IsNullOrWhiteSpace(info.Title))
                {
                    windows.Add(info);
                }
            }
            return true;
        }, IntPtr.Zero);

        return windows;
    }

    /// <inheritdoc />
    public WindowInfo? GetWindowAtPoint(Point point)
    {
        var nativePoint = new POINT(point.X, point.Y);
        IntPtr hwnd = WindowFromPoint(nativePoint);

        if (hwnd == IntPtr.Zero)
            return null;

        // Get the root window (top-level parent)
        IntPtr rootHwnd = GetAncestor(hwnd, GA_ROOT);
        if (rootHwnd != IntPtr.Zero)
        {
            hwnd = rootHwnd;
        }

        return CreateWindowInfo(hwnd);
    }

    /// <inheritdoc />
    public Rectangle GetWindowBounds(IntPtr hwnd)
    {
        // Try to get the extended frame bounds (more accurate with DWM)
        if (DwmGetWindowAttribute(hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT dwmRect, Marshal.SizeOf<RECT>()) == 0)
        {
            return dwmRect.ToRectangle();
        }

        // Fallback to GetWindowRect
        if (GetWindowRect(hwnd, out RECT rect))
        {
            return rect.ToRectangle();
        }

        return Rectangle.Empty;
    }

    /// <summary>
    /// Checks if a window is valid for enumeration.
    /// </summary>
    private bool IsValidWindow(IntPtr hwnd)
    {
        if (!IsWindowVisible(hwnd))
            return false;

        if (IsIconic(hwnd))
            return false;

        // Check window styles
        int style = GetWindowLong(hwnd, GWL_STYLE);
        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

        // Skip tool windows unless they have app window style
        if ((exStyle & WS_EX_TOOLWINDOW) != 0 && (exStyle & WS_EX_APPWINDOW) == 0)
            return false;

        // Check if window has valid size
        var bounds = GetWindowBounds(hwnd);
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return false;

        return true;
    }

    /// <summary>
    /// Creates a WindowInfo from a window handle.
    /// </summary>
    private WindowInfo? CreateWindowInfo(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return null;

        try
        {
            // Get window title
            int length = GetWindowTextLength(hwnd);
            string title = string.Empty;
            
            if (length > 0)
            {
                var sb = new StringBuilder(length + 1);
                GetWindowText(hwnd, sb, sb.Capacity);
                title = sb.ToString();
            }

            // Get bounds
            var bounds = GetWindowBounds(hwnd);

            // Get process name
            string processName = string.Empty;
            try
            {
                GetWindowThreadProcessId(hwnd, out uint processId);
                using var process = System.Diagnostics.Process.GetProcessById((int)processId);
                processName = process.ProcessName;
            }
            catch
            {
                // Process may have exited
            }

            return new WindowInfo
            {
                Handle = hwnd,
                Title = title,
                Bounds = bounds,
                ProcessName = processName,
                IsVisible = IsWindowVisible(hwnd),
                IsMinimized = IsIconic(hwnd)
            };
        }
        catch
        {
            return null;
        }
    }
}
