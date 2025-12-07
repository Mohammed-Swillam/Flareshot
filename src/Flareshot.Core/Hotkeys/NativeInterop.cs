using System.Runtime.InteropServices;

namespace Flareshot.Core.Hotkeys;

/// <summary>
/// P/Invoke declarations for Windows hotkey APIs.
/// </summary>
public static class NativeInterop
{
    /// <summary>
    /// Registers a system-wide hot key.
    /// </summary>
    /// <param name="hWnd">Handle to the window that will receive hot key messages.</param>
    /// <param name="id">Unique identifier for the hot key.</param>
    /// <param name="fsModifiers">Key modifiers (Alt, Ctrl, Shift, Win).</param>
    /// <param name="vk">Virtual key code.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    /// <summary>
    /// Unregisters a system-wide hot key.
    /// </summary>
    /// <param name="hWnd">Handle to the window that registered the hot key.</param>
    /// <param name="id">Unique identifier for the hot key to unregister.</param>
    /// <returns>True if successful, false otherwise.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    /// <summary>
    /// Windows message for hot key activation.
    /// </summary>
    public const int WM_HOTKEY = 0x0312;

    /// <summary>
    /// Modifier key flags for RegisterHotKey.
    /// </summary>
    public static class ModifierFlags
    {
        public const uint MOD_NONE = 0x0000;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000; // Prevents repeat messages while holding the key
    }
}

/// <summary>
/// Virtual key codes for common keys.
/// </summary>
public static class VirtualKeyCodes
{
    // Function keys
    public const int VK_F1 = 0x70;
    public const int VK_F2 = 0x71;
    public const int VK_F3 = 0x72;
    public const int VK_F4 = 0x73;
    public const int VK_F5 = 0x74;
    public const int VK_F6 = 0x75;
    public const int VK_F7 = 0x76;
    public const int VK_F8 = 0x77;
    public const int VK_F9 = 0x78;
    public const int VK_F10 = 0x79;
    public const int VK_F11 = 0x7A;
    public const int VK_F12 = 0x7B;

    // Special keys
    public const int VK_SNAPSHOT = 0x2C; // Print Screen
    public const int VK_INSERT = 0x2D;
    public const int VK_DELETE = 0x2E;
    public const int VK_HOME = 0x24;
    public const int VK_END = 0x23;
    public const int VK_PRIOR = 0x21; // Page Up
    public const int VK_NEXT = 0x22; // Page Down
    public const int VK_PAUSE = 0x13;
    public const int VK_SCROLL = 0x91; // Scroll Lock

    // Number keys
    public const int VK_0 = 0x30;
    public const int VK_1 = 0x31;
    public const int VK_2 = 0x32;
    public const int VK_3 = 0x33;
    public const int VK_4 = 0x34;
    public const int VK_5 = 0x35;
    public const int VK_6 = 0x36;
    public const int VK_7 = 0x37;
    public const int VK_8 = 0x38;
    public const int VK_9 = 0x39;

    // Letter keys (A-Z are 0x41-0x5A)
    public const int VK_A = 0x41;
    public const int VK_Z = 0x5A;

    /// <summary>
    /// Gets the display name for a virtual key code.
    /// </summary>
    public static string GetKeyName(int virtualKeyCode)
    {
        return virtualKeyCode switch
        {
            VK_SNAPSHOT => "Print Screen",
            VK_INSERT => "Insert",
            VK_DELETE => "Delete",
            VK_HOME => "Home",
            VK_END => "End",
            VK_PRIOR => "Page Up",
            VK_NEXT => "Page Down",
            VK_PAUSE => "Pause",
            VK_SCROLL => "Scroll Lock",
            >= VK_F1 and <= VK_F12 => $"F{virtualKeyCode - VK_F1 + 1}",
            >= VK_0 and <= VK_9 => ((char)virtualKeyCode).ToString(),
            >= VK_A and <= VK_Z => ((char)virtualKeyCode).ToString(),
            _ => $"Key {virtualKeyCode}"
        };
    }
}
