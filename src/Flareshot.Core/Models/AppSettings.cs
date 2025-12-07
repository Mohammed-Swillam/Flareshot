using System.Text.Json.Serialization;

namespace Flareshot.Core.Models;

/// <summary>
/// Represents the application settings that are persisted to disk.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// The virtual key code for the hotkey.
    /// Default is PrintScreen (VK_SNAPSHOT = 0x2C = 44).
    /// </summary>
    public int HotkeyKey { get; set; } = 44; // VK_SNAPSHOT

    /// <summary>
    /// Modifier keys for the hotkey (Alt, Ctrl, Shift, Win).
    /// </summary>
    public HotkeyModifiers HotkeyModifiers { get; set; } = HotkeyModifiers.None;

    /// <summary>
    /// Default folder path for saving screenshots.
    /// </summary>
    public string DefaultSaveFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

    /// <summary>
    /// Default image format for saving screenshots.
    /// </summary>
    public ImageFormat DefaultImageFormat { get; set; } = ImageFormat.Png;

    /// <summary>
    /// JPG quality setting (60-100).
    /// </summary>
    public int JpgQuality { get; set; } = 90;

    /// <summary>
    /// Whether to copy the screenshot to clipboard after capture.
    /// </summary>
    public bool CopyToClipboardAfterCapture { get; set; } = true;

    /// <summary>
    /// Whether to start the application minimized.
    /// </summary>
    public bool StartMinimized { get; set; } = true;

    /// <summary>
    /// Whether to auto-start the application with Windows.
    /// </summary>
    public bool AutoStartWithWindows { get; set; } = false;

    /// <summary>
    /// Creates a deep copy of the settings.
    /// </summary>
    public AppSettings Clone()
    {
        return new AppSettings
        {
            HotkeyKey = HotkeyKey,
            HotkeyModifiers = HotkeyModifiers,
            DefaultSaveFolder = DefaultSaveFolder,
            DefaultImageFormat = DefaultImageFormat,
            JpgQuality = JpgQuality,
            CopyToClipboardAfterCapture = CopyToClipboardAfterCapture,
            StartMinimized = StartMinimized,
            AutoStartWithWindows = AutoStartWithWindows
        };
    }
}

/// <summary>
/// Modifier keys for hotkey combinations.
/// </summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8
}

/// <summary>
/// Supported image formats for saving screenshots.
/// </summary>
public enum ImageFormat
{
    Png,
    Jpg
}
