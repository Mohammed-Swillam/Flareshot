# Flareshot

A lightweight, LightShot-style screen capture and annotation tool for Windows, built with WPF and .NET 8.0.

## Features (v0.1 MVP)

- 🎯 **Global Hotkey** - Trigger screen capture from anywhere
- ✂️ **Area Selection** - Select any region with visual feedback
- 🎨 **Annotation Tools** - Pencil, Line, Arrow, Rectangle, Marker, Text
- 🎨 **Color Picker** - Quick access to colors and brush sizes
- ↩️ **Undo/Redo** - Full history support for annotations
- 💾 **Save & Copy** - Export to PNG/JPG or copy to clipboard
- 📌 **System Tray** - Lives in tray for quick access
- ⚙️ **Settings** - Customizable hotkeys and preferences

## Architecture

- **ScreenCapture.Core** - Pure business logic (no UI dependencies)
- **ScreenCapture.UI** - WPF application with XAML views
- **ScreenCapture.Tests** - xUnit test project

## Requirements

- Windows 10/11
- .NET 8.0 Runtime

## Building

```bash
dotnet restore
dotnet build
```

## Running

```bash
dotnet run --project src/ScreenCapture.UI
```

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Print Screen` | Trigger capture (default, configurable) |
| `Escape` | Cancel capture |
| `Enter` | Confirm selection |
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `1-6` | Select tool (Pencil, Line, Arrow, Rectangle, Marker, Text) |
| `[` / `]` | Decrease/Increase brush size |

## License

MIT License
