using Microsoft.Win32;

namespace ScreenCapture.Core.Services;

/// <summary>
/// Manages auto-start with Windows functionality.
/// </summary>
public interface IAutoStartManager
{
    /// <summary>
    /// Gets whether auto-start is currently enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Enables auto-start with Windows.
    /// </summary>
    /// <param name="executablePath">Path to the application executable.</param>
    /// <param name="startMinimized">Whether to start minimized.</param>
    /// <returns>True if successful.</returns>
    bool Enable(string executablePath, bool startMinimized = true);

    /// <summary>
    /// Disables auto-start with Windows.
    /// </summary>
    /// <returns>True if successful.</returns>
    bool Disable();
}

/// <summary>
/// Manages auto-start registration in Windows registry.
/// Uses HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run
/// </summary>
public class AutoStartManager : IAutoStartManager
{
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "ScreenCapture.NET";

    /// <inheritdoc />
    public bool IsEnabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
                return key?.GetValue(AppName) != null;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <inheritdoc />
    public bool Enable(string executablePath, bool startMinimized = true)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key == null) return false;

            // Build command with --minimized flag if needed
            var command = startMinimized
                ? $"\"{executablePath}\" --minimized"
                : $"\"{executablePath}\"";

            key.SetValue(AppName, command);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key == null) return false;

            if (key.GetValue(AppName) != null)
            {
                key.DeleteValue(AppName, false);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
