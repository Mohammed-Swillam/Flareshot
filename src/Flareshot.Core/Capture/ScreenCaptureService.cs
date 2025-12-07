using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Flareshot.Core.Models;
using Screen = System.Windows.Forms.Screen;

namespace Flareshot.Core.Capture;

/// <summary>
/// Interface for screen capture operations.
/// </summary>
public interface IScreenCaptureService
{
    /// <summary>
    /// Gets information about all connected monitors.
    /// </summary>
    IReadOnlyList<MonitorInfo> GetAllMonitors();

    /// <summary>
    /// Gets the virtual screen bounds (union of all monitors).
    /// </summary>
    Rectangle GetVirtualScreenBounds();

    /// <summary>
    /// Captures the entire virtual screen (all monitors).
    /// </summary>
    Bitmap CaptureVirtualScreen();

    /// <summary>
    /// Captures a specific region of the screen.
    /// </summary>
    /// <param name="area">The area to capture in screen coordinates.</param>
    Bitmap CaptureArea(Rectangle area);

    /// <summary>
    /// Captures a specific window.
    /// </summary>
    /// <param name="hwnd">Handle to the window to capture.</param>
    Bitmap? CaptureWindow(IntPtr hwnd);
}

/// <summary>
/// Service for capturing screen content.
/// </summary>
public class ScreenCaptureService : IScreenCaptureService
{
    #region Native Methods

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest,
        IntPtr hdcSrc, int xSrc, int ySrc, int rop);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    private const int SRCCOPY = 0x00CC0020;
    private const int PW_RENDERFULLCONTENT = 0x00000002;
    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    #endregion

    /// <inheritdoc />
    public IReadOnlyList<MonitorInfo> GetAllMonitors()
    {
        var monitors = new List<MonitorInfo>();
        var allScreens = Screen.AllScreens;

        for (int i = 0; i < allScreens.Length; i++)
        {
            var screen = allScreens[i];
            var dpiScale = GetDpiScaleForScreen(screen);

            monitors.Add(new MonitorInfo
            {
                Index = i,
                Bounds = screen.Bounds,
                WorkingArea = screen.WorkingArea,
                DeviceName = screen.DeviceName,
                IsPrimary = screen.Primary,
                DpiScale = dpiScale
            });
        }

        return monitors;
    }

    /// <inheritdoc />
    public Rectangle GetVirtualScreenBounds()
    {
        int left = int.MaxValue, top = int.MaxValue;
        int right = int.MinValue, bottom = int.MinValue;

        foreach (var screen in Screen.AllScreens)
        {
            left = Math.Min(left, screen.Bounds.Left);
            top = Math.Min(top, screen.Bounds.Top);
            right = Math.Max(right, screen.Bounds.Right);
            bottom = Math.Max(bottom, screen.Bounds.Bottom);
        }

        return new Rectangle(left, top, right - left, bottom - top);
    }

    /// <inheritdoc />
    public Bitmap CaptureVirtualScreen()
    {
        var bounds = GetVirtualScreenBounds();
        return CaptureArea(bounds);
    }

    /// <inheritdoc />
    public Bitmap CaptureArea(Rectangle area)
    {
        var bitmap = new Bitmap(area.Width, area.Height, PixelFormat.Format32bppArgb);

        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(
                area.Left,
                area.Top,
                0,
                0,
                area.Size,
                CopyPixelOperation.SourceCopy);
        }

        return bitmap;
    }

    /// <inheritdoc />
    public Bitmap? CaptureWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return null;

        try
        {
            GetWindowRect(hwnd, out RECT rect);
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            if (width <= 0 || height <= 0)
                return null;

            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(bitmap))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    // Try PrintWindow first (works better with DWM)
                    if (!PrintWindow(hwnd, hdc, PW_RENDERFULLCONTENT))
                    {
                        // Fallback to BitBlt
                        IntPtr windowDC = GetWindowDC(hwnd);
                        try
                        {
                            BitBlt(hdc, 0, 0, width, height, windowDC, 0, 0, SRCCOPY);
                        }
                        finally
                        {
                            ReleaseDC(hwnd, windowDC);
                        }
                    }
                }
                finally
                {
                    graphics.ReleaseHdc(hdc);
                }
            }

            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the DPI scale factor for a screen.
    /// </summary>
    private double GetDpiScaleForScreen(Screen screen)
    {
        try
        {
            var point = new POINT
            {
                X = screen.Bounds.Left + screen.Bounds.Width / 2,
                Y = screen.Bounds.Top + screen.Bounds.Height / 2
            };

            IntPtr monitor = MonitorFromPoint(point, MONITOR_DEFAULTTONEAREST);
            
            if (GetDpiForMonitor(monitor, 0, out uint dpiX, out uint _) == 0)
            {
                return dpiX / 96.0;
            }
        }
        catch
        {
            // Fallback for older Windows versions
        }

        return 1.0;
    }
}
