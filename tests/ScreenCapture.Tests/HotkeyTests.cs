using ScreenCapture.Core.Hotkeys;
using ScreenCapture.Core.Models;

namespace ScreenCapture.Tests;

/// <summary>
/// Unit tests for GlobalHotkeyManager.
/// </summary>
public class GlobalHotkeyManagerTests
{
    [Fact]
    public void GetHotkeyDisplayString_WithNoModifiers_ReturnsKeyOnly()
    {
        // Arrange & Act
        var result = GlobalHotkeyManager.GetHotkeyDisplayString(
            VirtualKeyCodes.VK_SNAPSHOT, 
            HotkeyModifiers.None);

        // Assert
        Assert.Equal("Print Screen", result);
    }

    [Fact]
    public void GetHotkeyDisplayString_WithCtrlShift_ReturnsFormattedString()
    {
        // Arrange & Act
        var result = GlobalHotkeyManager.GetHotkeyDisplayString(
            VirtualKeyCodes.VK_F1,
            HotkeyModifiers.Control | HotkeyModifiers.Shift);

        // Assert
        Assert.Equal("Ctrl + Shift + F1", result);
    }

    [Fact]
    public void GetHotkeyDisplayString_WithAllModifiers_ReturnsFormattedString()
    {
        // Arrange & Act
        var result = GlobalHotkeyManager.GetHotkeyDisplayString(
            VirtualKeyCodes.VK_A,
            HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift | HotkeyModifiers.Win);

        // Assert
        Assert.Equal("Ctrl + Alt + Shift + Win + A", result);
    }
}

/// <summary>
/// Unit tests for VirtualKeyCodes.
/// </summary>
public class VirtualKeyCodesTests
{
    [Theory]
    [InlineData(VirtualKeyCodes.VK_SNAPSHOT, "Print Screen")]
    [InlineData(VirtualKeyCodes.VK_F1, "F1")]
    [InlineData(VirtualKeyCodes.VK_F12, "F12")]
    [InlineData(VirtualKeyCodes.VK_INSERT, "Insert")]
    [InlineData(VirtualKeyCodes.VK_DELETE, "Delete")]
    [InlineData(VirtualKeyCodes.VK_HOME, "Home")]
    [InlineData(VirtualKeyCodes.VK_END, "End")]
    [InlineData(VirtualKeyCodes.VK_A, "A")]
    [InlineData(VirtualKeyCodes.VK_0, "0")]
    public void GetKeyName_ReturnsCorrectName(int keyCode, string expectedName)
    {
        // Act
        var result = VirtualKeyCodes.GetKeyName(keyCode);

        // Assert
        Assert.Equal(expectedName, result);
    }

    [Fact]
    public void GetKeyName_ForUnknownKey_ReturnsKeyNumber()
    {
        // Arrange
        const int unknownKey = 0xFF;

        // Act
        var result = VirtualKeyCodes.GetKeyName(unknownKey);

        // Assert
        Assert.StartsWith("Key ", result);
    }
}
