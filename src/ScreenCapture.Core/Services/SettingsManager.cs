using System.Text.Json;
using ScreenCapture.Core.Models;

namespace ScreenCapture.Core.Services;

/// <summary>
/// Interface for settings management operations.
/// </summary>
public interface ISettingsManager
{
    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    AppSettings CurrentSettings { get; }

    /// <summary>
    /// Loads settings from disk.
    /// </summary>
    Task<AppSettings> LoadAsync();

    /// <summary>
    /// Saves settings to disk.
    /// </summary>
    Task SaveAsync(AppSettings settings);

    /// <summary>
    /// Resets settings to defaults.
    /// </summary>
    AppSettings ResetToDefaults();

    /// <summary>
    /// Event raised when settings are changed.
    /// </summary>
    event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
}

/// <summary>
/// Event args for settings change notifications.
/// </summary>
public class SettingsChangedEventArgs : EventArgs
{
    public AppSettings OldSettings { get; }
    public AppSettings NewSettings { get; }

    public SettingsChangedEventArgs(AppSettings oldSettings, AppSettings newSettings)
    {
        OldSettings = oldSettings;
        NewSettings = newSettings;
    }
}

/// <summary>
/// Manages loading, saving, and persistence of application settings.
/// Settings are stored in %APPDATA%\ScreenCapture.NET\settings.json
/// </summary>
public class SettingsManager : ISettingsManager
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ScreenCapture.NET");

    private static readonly string SettingsFilePath = Path.Combine(AppDataFolder, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private AppSettings _currentSettings = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public AppSettings CurrentSettings
    {
        get
        {
            lock (_lock)
            {
                return _currentSettings.Clone();
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    /// <inheritdoc />
    public async Task<AppSettings> LoadAsync()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                // No settings file exists, create default and save
                var defaultSettings = new AppSettings();
                await SaveAsync(defaultSettings);
                return defaultSettings;
            }

            var json = await File.ReadAllTextAsync(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

            if (settings == null)
            {
                // Corrupted file, reset to defaults
                return await HandleCorruptedSettingsAsync();
            }

            lock (_lock)
            {
                _currentSettings = settings;
            }

            return settings.Clone();
        }
        catch (JsonException)
        {
            // Corrupted JSON, reset to defaults
            return await HandleCorruptedSettingsAsync();
        }
        catch (Exception)
        {
            // Any other error, return defaults without saving
            return new AppSettings();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(AppSettings settings)
    {
        // Ensure directory exists
        Directory.CreateDirectory(AppDataFolder);

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(SettingsFilePath, json);

        AppSettings oldSettings;
        lock (_lock)
        {
            oldSettings = _currentSettings.Clone();
            _currentSettings = settings.Clone();
        }

        // Raise change notification
        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(oldSettings, settings.Clone()));
    }

    /// <inheritdoc />
    public AppSettings ResetToDefaults()
    {
        var defaultSettings = new AppSettings();
        
        lock (_lock)
        {
            var oldSettings = _currentSettings.Clone();
            _currentSettings = defaultSettings;
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(oldSettings, defaultSettings.Clone()));
        }

        // Fire and forget save operation
        _ = Task.Run(async () =>
        {
            try
            {
                await SaveAsync(defaultSettings);
            }
            catch
            {
                // Ignore save errors during reset
            }
        });

        return defaultSettings.Clone();
    }

    private async Task<AppSettings> HandleCorruptedSettingsAsync()
    {
        var defaultSettings = new AppSettings();
        
        try
        {
            // Backup corrupted file
            if (File.Exists(SettingsFilePath))
            {
                var backupPath = SettingsFilePath + ".corrupted";
                File.Move(SettingsFilePath, backupPath, overwrite: true);
            }

            await SaveAsync(defaultSettings);
        }
        catch
        {
            // Ignore errors during recovery
        }

        lock (_lock)
        {
            _currentSettings = defaultSettings;
        }

        return defaultSettings.Clone();
    }

    /// <summary>
    /// Gets the settings directory path.
    /// </summary>
    public static string GetSettingsDirectory() => AppDataFolder;

    /// <summary>
    /// Gets the settings file path.
    /// </summary>
    public static string GetSettingsFilePath() => SettingsFilePath;
}
