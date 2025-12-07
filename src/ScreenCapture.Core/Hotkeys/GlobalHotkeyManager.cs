using ScreenCapture.Core.Models;

namespace ScreenCapture.Core.Hotkeys;

/// <summary>
/// Event arguments for hotkey pressed events.
/// </summary>
public class HotkeyEventArgs : EventArgs
{
    /// <summary>
    /// The hotkey ID that was pressed.
    /// </summary>
    public int HotkeyId { get; }

    /// <summary>
    /// The virtual key code that was pressed.
    /// </summary>
    public int VirtualKeyCode { get; }

    /// <summary>
    /// The modifier keys that were held.
    /// </summary>
    public HotkeyModifiers Modifiers { get; }

    public HotkeyEventArgs(int hotkeyId, int virtualKeyCode, HotkeyModifiers modifiers)
    {
        HotkeyId = hotkeyId;
        VirtualKeyCode = virtualKeyCode;
        Modifiers = modifiers;
    }
}

/// <summary>
/// Interface for global hotkey management.
/// </summary>
public interface IGlobalHotkeyManager : IDisposable
{
    /// <summary>
    /// Registers a global hotkey.
    /// </summary>
    /// <param name="id">Unique identifier for the hotkey.</param>
    /// <param name="virtualKeyCode">The virtual key code.</param>
    /// <param name="modifiers">The modifier keys.</param>
    /// <returns>True if registration succeeded, false otherwise.</returns>
    bool RegisterHotkey(int id, int virtualKeyCode, HotkeyModifiers modifiers);

    /// <summary>
    /// Unregisters a global hotkey.
    /// </summary>
    /// <param name="id">The hotkey ID to unregister.</param>
    /// <returns>True if unregistration succeeded, false otherwise.</returns>
    bool UnregisterHotkey(int id);

    /// <summary>
    /// Unregisters all registered hotkeys.
    /// </summary>
    void UnregisterAllHotkeys();

    /// <summary>
    /// Event raised when a registered hotkey is pressed.
    /// </summary>
    event EventHandler<HotkeyEventArgs>? HotkeyPressed;

    /// <summary>
    /// Processes a Windows message to check for hotkey activation.
    /// Call this from your window's message handler.
    /// </summary>
    /// <param name="msg">The Windows message ID.</param>
    /// <param name="wParam">The wParam of the message.</param>
    /// <param name="lParam">The lParam of the message.</param>
    /// <returns>True if the message was handled as a hotkey, false otherwise.</returns>
    bool ProcessMessage(int msg, IntPtr wParam, IntPtr lParam);
}

/// <summary>
/// Manages global hotkey registration and event handling.
/// </summary>
public class GlobalHotkeyManager : IGlobalHotkeyManager
{
    private readonly IntPtr _windowHandle;
    private readonly Dictionary<int, (int VirtualKeyCode, HotkeyModifiers Modifiers)> _registeredHotkeys = new();
    private bool _disposed;

    /// <inheritdoc />
    public event EventHandler<HotkeyEventArgs>? HotkeyPressed;

    /// <summary>
    /// Creates a new GlobalHotkeyManager.
    /// </summary>
    /// <param name="windowHandle">Handle to the window that will receive hotkey messages.</param>
    public GlobalHotkeyManager(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
    }

    /// <inheritdoc />
    public bool RegisterHotkey(int id, int virtualKeyCode, HotkeyModifiers modifiers)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Unregister if already registered
        if (_registeredHotkeys.ContainsKey(id))
        {
            UnregisterHotkey(id);
        }

        // Convert our modifiers to Windows modifier flags
        uint nativeModifiers = ConvertModifiers(modifiers);

        // Add MOD_NOREPEAT to prevent repeated messages while holding the key
        nativeModifiers |= NativeInterop.ModifierFlags.MOD_NOREPEAT;

        bool success = NativeInterop.RegisterHotKey(_windowHandle, id, nativeModifiers, (uint)virtualKeyCode);

        if (success)
        {
            _registeredHotkeys[id] = (virtualKeyCode, modifiers);
        }

        return success;
    }

    /// <inheritdoc />
    public bool UnregisterHotkey(int id)
    {
        if (_disposed) return false;

        if (!_registeredHotkeys.ContainsKey(id))
        {
            return true; // Already unregistered
        }

        bool success = NativeInterop.UnregisterHotKey(_windowHandle, id);
        
        if (success)
        {
            _registeredHotkeys.Remove(id);
        }

        return success;
    }

    /// <inheritdoc />
    public void UnregisterAllHotkeys()
    {
        if (_disposed) return;

        foreach (var id in _registeredHotkeys.Keys.ToList())
        {
            UnregisterHotkey(id);
        }
    }

    /// <inheritdoc />
    public bool ProcessMessage(int msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg != NativeInterop.WM_HOTKEY)
        {
            return false;
        }

        int hotkeyId = wParam.ToInt32();

        if (_registeredHotkeys.TryGetValue(hotkeyId, out var hotkeyInfo))
        {
            HotkeyPressed?.Invoke(this, new HotkeyEventArgs(
                hotkeyId,
                hotkeyInfo.VirtualKeyCode,
                hotkeyInfo.Modifiers));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Converts our HotkeyModifiers enum to Windows modifier flags.
    /// </summary>
    private static uint ConvertModifiers(HotkeyModifiers modifiers)
    {
        uint result = NativeInterop.ModifierFlags.MOD_NONE;

        if (modifiers.HasFlag(HotkeyModifiers.Alt))
            result |= NativeInterop.ModifierFlags.MOD_ALT;

        if (modifiers.HasFlag(HotkeyModifiers.Control))
            result |= NativeInterop.ModifierFlags.MOD_CONTROL;

        if (modifiers.HasFlag(HotkeyModifiers.Shift))
            result |= NativeInterop.ModifierFlags.MOD_SHIFT;

        if (modifiers.HasFlag(HotkeyModifiers.Win))
            result |= NativeInterop.ModifierFlags.MOD_WIN;

        return result;
    }

    /// <summary>
    /// Gets a display string for the hotkey combination.
    /// </summary>
    public static string GetHotkeyDisplayString(int virtualKeyCode, HotkeyModifiers modifiers)
    {
        var parts = new List<string>();

        if (modifiers.HasFlag(HotkeyModifiers.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(HotkeyModifiers.Win)) parts.Add("Win");

        parts.Add(VirtualKeyCodes.GetKeyName(virtualKeyCode));

        return string.Join(" + ", parts);
    }

    public void Dispose()
    {
        if (_disposed) return;

        UnregisterAllHotkeys();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
