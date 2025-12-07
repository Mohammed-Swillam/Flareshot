using Flareshot.Core.Models;

namespace Flareshot.Tests;

/// <summary>
/// Unit tests for AppSettings.
/// </summary>
public class AppSettingsTests
{
    [Fact]
    public void Clone_ShouldCreateIndependentCopy()
    {
        // Arrange
        var original = new AppSettings
        {
            HotkeyKey = 123,
            DefaultSaveFolder = "C:\\Test",
            JpgQuality = 75
        };

        // Act
        var clone = original.Clone();
        clone.HotkeyKey = 456;
        clone.DefaultSaveFolder = "C:\\Other";

        // Assert
        Assert.Equal(123, original.HotkeyKey);
        Assert.Equal("C:\\Test", original.DefaultSaveFolder);
        Assert.Equal(456, clone.HotkeyKey);
        Assert.Equal("C:\\Other", clone.DefaultSaveFolder);
    }

    [Fact]
    public void DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        Assert.Equal(44, settings.HotkeyKey); // VK_SNAPSHOT
        Assert.Equal(HotkeyModifiers.None, settings.HotkeyModifiers);
        Assert.Equal(90, settings.JpgQuality);
        Assert.True(settings.CopyToClipboardAfterCapture);
        Assert.True(settings.StartMinimized);
        Assert.False(settings.AutoStartWithWindows);
    }

    [Fact]
    public void HotkeyModifiers_ShouldSupportCombinations()
    {
        // Arrange
        var settings = new AppSettings
        {
            HotkeyModifiers = HotkeyModifiers.Control | HotkeyModifiers.Shift
        };

        // Assert
        Assert.True(settings.HotkeyModifiers.HasFlag(HotkeyModifiers.Control));
        Assert.True(settings.HotkeyModifiers.HasFlag(HotkeyModifiers.Shift));
        Assert.False(settings.HotkeyModifiers.HasFlag(HotkeyModifiers.Alt));
        Assert.False(settings.HotkeyModifiers.HasFlag(HotkeyModifiers.Win));
    }
}
