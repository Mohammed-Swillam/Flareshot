using ScreenCapture.Core.Models;
using ScreenCapture.Core.Services;

namespace ScreenCapture.Tests;

/// <summary>
/// Unit tests for SettingsManager.
/// </summary>
public class SettingsManagerTests
{
    [Fact]
    public async Task LoadAsync_WithNoFile_ReturnsDefaults()
    {
        // Arrange
        var manager = new SettingsManager();

        // Act
        var settings = await manager.LoadAsync();

        // Assert
        Assert.NotNull(settings);
        Assert.Equal(44, settings.HotkeyKey); // Default: PrintScreen
    }

    [Fact]
    public async Task SaveAsync_ThenLoad_ReturnsSavedValues()
    {
        // Arrange
        var manager = new SettingsManager();
        var settings = new AppSettings
        {
            HotkeyKey = 120, // F9
            JpgQuality = 85
        };

        // Act
        await manager.SaveAsync(settings);
        var loaded = await manager.LoadAsync();

        // Assert
        Assert.Equal(120, loaded.HotkeyKey);
        Assert.Equal(85, loaded.JpgQuality);
    }

    [Fact]
    public void ResetToDefaults_ShouldReturnDefaultSettings()
    {
        // Arrange
        var manager = new SettingsManager();

        // Act
        var defaults = manager.ResetToDefaults();

        // Assert
        Assert.Equal(44, defaults.HotkeyKey);
        Assert.Equal(90, defaults.JpgQuality);
    }

    [Fact]
    public async Task SettingsChanged_ShouldFireOnSave()
    {
        // Arrange
        var manager = new SettingsManager();
        var eventFired = false;
        AppSettings? newSettings = null;

        manager.SettingsChanged += (sender, args) =>
        {
            eventFired = true;
            newSettings = args.NewSettings;
        };

        // Act
        await manager.SaveAsync(new AppSettings { JpgQuality = 70 });

        // Assert
        Assert.True(eventFired);
        Assert.NotNull(newSettings);
        Assert.Equal(70, newSettings!.JpgQuality);
    }
}
