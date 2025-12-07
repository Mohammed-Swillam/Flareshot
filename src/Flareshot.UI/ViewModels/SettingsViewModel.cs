using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Flareshot.Core.Models;
using Flareshot.Core.Services;

namespace Flareshot.UI.ViewModels;

/// <summary>
/// ViewModel for the Settings window.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsManager _settingsManager;
    private AppSettings _originalSettings;

    // General Settings
    [ObservableProperty]
    private bool _startMinimized;

    [ObservableProperty]
    private bool _autoStartWithWindows;

    // Capture Settings
    [ObservableProperty]
    private int _hotkeyKey;

    [ObservableProperty]
    private bool _hotkeyAlt;

    [ObservableProperty]
    private bool _hotkeyCtrl;

    [ObservableProperty]
    private bool _hotkeyShift;

    [ObservableProperty]
    private bool _hotkeyWin;

    [ObservableProperty]
    private bool _isRecordingHotkey;

    [ObservableProperty]
    private string _hotkeyDisplayText = "Press a key...";

    // Save Settings
    [ObservableProperty]
    private string _defaultSaveFolder = string.Empty;

    [ObservableProperty]
    private Core.Models.ImageFormat _defaultImageFormat;

    [ObservableProperty]
    private int _jpgQuality;

    // Behavior Settings
    [ObservableProperty]
    private bool _copyToClipboardAfterCapture;

    // State
    [ObservableProperty]
    private bool _hasChanges;

    public SettingsViewModel(ISettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
        _originalSettings = settingsManager.CurrentSettings;

        LoadSettingsFromModel(_originalSettings);
        UpdateHotkeyDisplayText();
    }

    /// <summary>
    /// Load settings from the model into ViewModel properties.
    /// </summary>
    private void LoadSettingsFromModel(AppSettings settings)
    {
        StartMinimized = settings.StartMinimized;
        AutoStartWithWindows = settings.AutoStartWithWindows;
        
        HotkeyKey = settings.HotkeyKey;
        HotkeyAlt = settings.HotkeyModifiers.HasFlag(HotkeyModifiers.Alt);
        HotkeyCtrl = settings.HotkeyModifiers.HasFlag(HotkeyModifiers.Control);
        HotkeyShift = settings.HotkeyModifiers.HasFlag(HotkeyModifiers.Shift);
        HotkeyWin = settings.HotkeyModifiers.HasFlag(HotkeyModifiers.Win);

        DefaultSaveFolder = settings.DefaultSaveFolder;
        DefaultImageFormat = settings.DefaultImageFormat;
        JpgQuality = settings.JpgQuality;

        CopyToClipboardAfterCapture = settings.CopyToClipboardAfterCapture;

        HasChanges = false;
    }

    /// <summary>
    /// Create AppSettings from ViewModel properties.
    /// </summary>
    private AppSettings CreateSettingsFromViewModel()
    {
        var modifiers = HotkeyModifiers.None;
        if (HotkeyAlt) modifiers |= HotkeyModifiers.Alt;
        if (HotkeyCtrl) modifiers |= HotkeyModifiers.Control;
        if (HotkeyShift) modifiers |= HotkeyModifiers.Shift;
        if (HotkeyWin) modifiers |= HotkeyModifiers.Win;

        return new AppSettings
        {
            StartMinimized = StartMinimized,
            AutoStartWithWindows = AutoStartWithWindows,
            HotkeyKey = HotkeyKey,
            HotkeyModifiers = modifiers,
            DefaultSaveFolder = DefaultSaveFolder,
            DefaultImageFormat = DefaultImageFormat,
            JpgQuality = JpgQuality,
            CopyToClipboardAfterCapture = CopyToClipboardAfterCapture
        };
    }

    /// <summary>
    /// Update the hotkey display text based on current settings.
    /// </summary>
    private void UpdateHotkeyDisplayText()
    {
        var parts = new List<string>();

        if (HotkeyCtrl) parts.Add("Ctrl");
        if (HotkeyAlt) parts.Add("Alt");
        if (HotkeyShift) parts.Add("Shift");
        if (HotkeyWin) parts.Add("Win");

        var keyName = GetKeyName(HotkeyKey);
        if (!string.IsNullOrEmpty(keyName))
        {
            parts.Add(keyName);
        }

        HotkeyDisplayText = parts.Count > 0 ? string.Join(" + ", parts) : "None";
    }

    /// <summary>
    /// Get display name for a virtual key code.
    /// </summary>
    private static string GetKeyName(int virtualKeyCode)
    {
        // Common key mappings
        return virtualKeyCode switch
        {
            44 => "Print Screen",
            112 => "F1",
            113 => "F2",
            114 => "F3",
            115 => "F4",
            116 => "F5",
            117 => "F6",
            118 => "F7",
            119 => "F8",
            120 => "F9",
            121 => "F10",
            122 => "F11",
            123 => "F12",
            45 => "Insert",
            36 => "Home",
            35 => "End",
            33 => "Page Up",
            34 => "Page Down",
            19 => "Pause",
            145 => "Scroll Lock",
            _ when virtualKeyCode >= 65 && virtualKeyCode <= 90 => ((char)virtualKeyCode).ToString(),
            _ when virtualKeyCode >= 48 && virtualKeyCode <= 57 => ((char)virtualKeyCode).ToString(),
            _ => $"Key {virtualKeyCode}"
        };
    }

    /// <summary>
    /// Handle key recording for hotkey setting.
    /// </summary>
    public void RecordHotkey(Key key)
    {
        if (!IsRecordingHotkey) return;

        // Ignore modifier keys by themselves
        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        // Convert WPF Key to virtual key code
        HotkeyKey = KeyInterop.VirtualKeyFromKey(key);
        IsRecordingHotkey = false;
        UpdateHotkeyDisplayText();
        HasChanges = true;
    }

    /// <summary>
    /// Starts recording a hotkey.
    /// </summary>
    [RelayCommand]
    private void StartRecordingHotkey()
    {
        IsRecordingHotkey = true;
        HotkeyDisplayText = "Press a key...";
    }

    /// <summary>
    /// Browse for save folder.
    /// </summary>
    [RelayCommand]
    private void BrowseFolder()
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select default save folder",
            SelectedPath = DefaultSaveFolder,
            ShowNewFolderButton = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            DefaultSaveFolder = dialog.SelectedPath;
            HasChanges = true;
        }
    }

    /// <summary>
    /// Save settings.
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        var settings = CreateSettingsFromViewModel();
        await _settingsManager.SaveAsync(settings);
        _originalSettings = settings;
        HasChanges = false;
    }

    /// <summary>
    /// Reset settings to original values.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        LoadSettingsFromModel(_originalSettings);
        UpdateHotkeyDisplayText();
    }

    /// <summary>
    /// Apply settings without closing.
    /// </summary>
    [RelayCommand]
    private async Task ApplyAsync()
    {
        await SaveAsync();
    }

    /// <summary>
    /// Called when any property changes to track if there are unsaved changes.
    /// </summary>
    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        // Track changes for most properties
        if (e.PropertyName != nameof(HasChanges) && 
            e.PropertyName != nameof(IsRecordingHotkey) &&
            e.PropertyName != nameof(HotkeyDisplayText) &&
            e.PropertyName != nameof(IsBusy))
        {
            HasChanges = true;
            
            // Update hotkey display when modifiers change
            if (e.PropertyName == nameof(HotkeyAlt) ||
                e.PropertyName == nameof(HotkeyCtrl) ||
                e.PropertyName == nameof(HotkeyShift) ||
                e.PropertyName == nameof(HotkeyWin))
            {
                UpdateHotkeyDisplayText();
            }
        }
    }
}
