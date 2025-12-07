<p align="center">
  <img src="assets/flareshot_app_icon.png" alt="Flareshot Logo" width="120" height="120">
</p>

<h1 align="center">Flareshot</h1>

<p align="center">
  <strong>🔥 A fast, lightweight screenshot and annotation tool for Windows</strong>
</p>

<p align="center">
  <a href="#features">Features</a> •
  <a href="#installation">Installation</a> •
  <a href="#usage">Usage</a> •
  <a href="#keyboard-shortcuts">Shortcuts</a> •
  <a href="#building">Building</a>
</p>

---

## ✨ What is Flareshot?

Flareshot is a modern, lightweight screen capture tool for Windows that lets you quickly capture any area of your screen and annotate it with arrows, text, shapes, and more. Inspired by LightShot, built with modern .NET 8.0 and WPF.

**Perfect for:**
- 📝 Creating quick tutorials and documentation
- 🐛 Capturing and annotating bugs for developers
- 💬 Sharing visual feedback with teammates
- 📧 Adding context to emails and messages

---

## 🚀 Features

### Capture
- 🎯 **Global Hotkey** - Press `Print Screen` from anywhere to start capturing
- ✂️ **Area Selection** - Click and drag to select any region
- 📐 **Resize & Move** - Adjust your selection with handles before annotating
- 🖥️ **Multi-Monitor** - Works seamlessly across multiple displays

### Annotate
- ✏️ **Pencil** - Freehand drawing for quick sketches
- ➡️ **Arrows** - Point out important elements
- ▢ **Rectangles** - Highlight areas of interest
- 📝 **Text** - Add labels and descriptions
- 🖍️ **Marker** - Semi-transparent highlighter effect
- 📏 **Lines** - Draw straight lines

### Productivity
- ↩️ **Undo/Redo** - Full history support (Ctrl+Z / Ctrl+Y)
- 🎨 **Color Picker** - Choose from preset colors or custom RGB
- 💾 **Save** - Export as PNG or JPEG (Ctrl+S)
- 📋 **Copy** - Send directly to clipboard (Enter or Ctrl+C)
- 📌 **System Tray** - Lives quietly in your tray, always ready
- ⚙️ **Customizable** - Configure hotkeys, save location, and more

---

## 📥 Installation

### Requirements
- Windows 10/11 (64-bit)
- .NET 8.0 Runtime

### Quick Start

1. Download the latest release from [Releases](https://github.com/Mohammed-Swillam/Flareshot/releases)
2. Extract and run `Flareshot.UI.exe`
3. Press `Print Screen` to capture!

---

## 🎮 Usage

1. **Start Capture**: Press `Print Screen` (or your configured hotkey)
2. **Select Area**: Click and drag to select the region you want to capture
3. **Annotate**: Use the toolbar on the right to add arrows, text, shapes, etc.
4. **Save or Copy**: 
   - Press `Enter` or click Copy to send to clipboard
   - Press `Ctrl+S` or click Save to save to file

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Print Screen` | Start capture (configurable) |
| `Escape` | Cancel and close |
| `Enter` | Confirm and copy to clipboard |
| `Ctrl+S` | Save to file |
| `Ctrl+C` | Copy to clipboard |
| `Ctrl+Z` | Undo last annotation |
| `Ctrl+Y` | Redo |
| `P` | Pencil tool |
| `L` | Line tool |
| `A` | Arrow tool |
| `R` | Rectangle tool |
| `M` | Marker tool |
| `T` | Text tool |

---

## 🏗️ Building from Source

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 or VS Code (optional)

### Build

```bash
# Clone the repository
git clone https://github.com/Mohammed-Swillam/Flareshot.git
cd Flareshot

# Restore and build
dotnet build Flareshot.sln

# Run
dotnet run --project src/Flareshot.UI
```

### Run Tests

```bash
dotnet test Flareshot.sln
```

---

## 📁 Project Structure

```
Flareshot/
├── src/
│   ├── Flareshot.Core/      # Business logic (no UI dependencies)
│   │   ├── Capture/         # Screen capture services
│   │   ├── Drawing/         # Annotation models & commands
│   │   ├── Hotkeys/         # Global hotkey management
│   │   ├── IO/              # File export & clipboard
│   │   └── Services/        # Settings & auto-start
│   └── Flareshot.UI/        # WPF Application
│       ├── Controls/        # Custom controls (toolbars, canvas)
│       ├── Views/           # Windows (overlay, settings)
│       └── ViewModels/      # MVVM view models
├── tests/
│   └── Flareshot.Tests/     # Unit tests (xUnit)
├── docs/                    # Documentation
└── requirements/            # Specifications
```

---

## 🛣️ Roadmap

### v0.1 (Current)
- ✅ Area selection with visual feedback
- ✅ Annotation tools (pencil, arrow, rectangle, text, marker, line)
- ✅ Undo/redo functionality
- ✅ Save as PNG/JPEG
- ✅ Copy to clipboard
- ✅ System tray integration
- ✅ Settings window
- ✅ Customizable hotkeys

### v0.2 (Planned)
- 🔲 Blur/pixelate tool
- 🔲 Screenshot history
- 🔲 Numbered steps tool
- 🔲 Image resize before save
- 🔲 Auto-upload to cloud

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

<p align="center">
  Made with ❤️ for Windows users who need quick screenshots
</p>
