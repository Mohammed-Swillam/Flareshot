using Flareshot.Core.Models;
using Flareshot.Core.Services;

namespace Flareshot.Tests;

/// <summary>
/// Unit tests for SettingsManager.
/// Note: These tests share a common settings file, so they run in a Collection to avoid parallelism issues.
/// </summary>
[Collection("SettingsTests")]
public class SettingsManagerTests
{
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
        
        // Create new manager to ensure we're reading from disk
        var manager2 = new SettingsManager();
        var loaded = await manager2.LoadAsync();

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

        // Wait a moment to avoid file lock from previous test
        await Task.Delay(100);

        // Act
        await manager.SaveAsync(new AppSettings { JpgQuality = 70 });

        // Assert
        Assert.True(eventFired);
        Assert.NotNull(newSettings);
        Assert.Equal(70, newSettings!.JpgQuality);
    }

    [Fact]
    public void CurrentSettings_ShouldReturnClone()
    {
        // Arrange
        var manager = new SettingsManager();

        // Act
        var settings1 = manager.CurrentSettings;
        var settings2 = manager.CurrentSettings;
        settings1.JpgQuality = 50;

        // Assert - modifying returned settings should not affect original
        Assert.NotEqual(settings1.JpgQuality, settings2.JpgQuality);
    }
}
