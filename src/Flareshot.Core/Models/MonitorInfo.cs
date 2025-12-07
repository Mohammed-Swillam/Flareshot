using System.Drawing;

namespace Flareshot.Core.Models;

/// <summary>
/// Represents information about a monitor/display.
/// </summary>
public class MonitorInfo
{
    /// <summary>
    /// The bounds of the monitor in screen coordinates.
    /// </summary>
    public Rectangle Bounds { get; set; }

    /// <summary>
    /// The working area (excluding taskbar) of the monitor.
    /// </summary>
    public Rectangle WorkingArea { get; set; }

    /// <summary>
    /// The device name of the monitor.
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the primary monitor.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// The DPI scaling factor for this monitor (1.0 = 100%, 1.5 = 150%, etc.)
    /// </summary>
    public double DpiScale { get; set; } = 1.0;

    /// <summary>
    /// The index of this monitor (0-based).
    /// </summary>
    public int Index { get; set; }
}
