# LightShot Clone - Windows Desktop App
## Comprehensive Developer Plan (v0.1 MVP)

**Project Name:** ScreenCapture.NET  
**Target Platform:** Windows 10/11 (64-bit)  
**Target Framework:** .NET 8.0+ 
**UI Framework:** WPF (Windows Presentation Foundation)  
**Language:** C#  

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Project Scope](#project-scope)
3. [Architecture Overview](#architecture-overview)
4. [Detailed Phase Breakdown](#detailed-phase-breakdown)
5. [Technical Specifications](#technical-specifications)
6. [Data Models](#data-models)
7. [File Structure](#file-structure)
8. [Dependencies & NuGet Packages](#dependencies--nuget-packages)
9. [Key Algorithms & Implementations](#key-algorithms--implementations)
10. [Testing Strategy](#testing-strategy)
11. [Known Challenges & Solutions](#known-challenges--solutions)
12. [Keyboard Shortcuts Reference](#keyboard-shortcuts-reference)
13. [Error Handling & User Messages](#error-handling--user-messages)
14. [Deliverables Checklist](#deliverables-checklist)
15. [Version 0.2 Roadmap](#version-02-roadmap)

---

## Executive Summary

This document outlines the development plan for a Windows desktop screenshot tool (LightShot clone) with annotation capabilities. The project follows a phased approach:

- **v0.1 (MVP):** Core screenshot capture + basic annotation editor
- **v0.2+:** Extended features (blur, highlight, settings, history)

**Estimated Duration:** 6-8 weeks for v0.1 (working full-time)

---

## Project Scope

### v0.1 Features (In Scope)

#### Screenshot Capture
- [x] Global hotkey activation (Print Screen key, configurable later)
- [x] Area selection with visual feedback
- [x] Full screen capture
- [x] Specific window capture
- [x] Multi-monitor support
- [x] Screen dimming during selection
- [x] Real-time selection preview with resize handles

##### Window Capture Workflow
1. User presses hotkey → overlay appears
2. User presses **Alt key** (or clicks "Window" button) → enters window selection mode
3. Hovering over windows highlights them with a colored border
4. Clicking a window captures that window only
5. **Edge cases:**
   - Minimized windows: Skip (cannot capture minimized windows)
   - Off-screen windows: Warn user with toast notification
   - Windows with transparency: Capture with solid background (white)
   - Windows behind other windows: Capture visible content only (no z-order tricks in v0.1)

##### Selection Behavior Details
- **Minimum selection size:** 10×10 pixels (prevent accidental micro-clicks)
- **Cross-monitor selection:** Allowed, capture area spans across monitors
- **Move selection:** User can click inside selection box and drag to reposition before confirming
- **Crosshair cursor:** Show crosshair cursor until mouse down, then show normal cursor
- **Arrow key nudging:** After drawing selection, use arrow keys to move by 1px (Shift+Arrow for 10px)

##### Overlay Appearance
- Full screen overlay with **50% black opacity** (dimmed background)
- Selected area: Clear/transparent (shows actual screen content)
- Non-selected area: Dimmed (50% black overlay remains)
- Selection border: **2px dashed line** (alternating blue and white for visibility on any background)
- Resize handles: **8px square boxes** at corners and edge midpoints

#### Quick Action Toolbar
- [x] Edit button → Open full editor
- [x] Save button → Save to disk (PNG/JPG)
- [x] Copy button → Copy to clipboard
- [x] Cancel button → Discard capture

#### Annotation Editor
- [x] Pencil/Freehand drawing tool
- [x] Arrow tool (straight lines with arrowhead)
- [x] Rectangle tool (hollow/outlined)
- [x] Text tool with editable text boxes
- [x] Color picker (predefined colors + custom)
- [x] Brush size adjustment
- [x] Font size adjustment
- [x] Undo/Redo functionality (Ctrl+Z / Ctrl+Y)
- [x] Save as PNG or JPG
- [x] Copy edited image to clipboard

#### System Integration
- [x] System tray icon (minimize to tray)
- [x] Auto-start on Windows boot (optional setting)
- [x] Portable executable (no installer for v0.1)

##### Default Save Location & Filename Strategy
- **Default folder:** `%USERPROFILE%\Pictures\Screenshots\`
- **Filename format:** `Screenshot_YYYY-MM-DD_HH-mm-ss.png`
- **Behavior:** Create folder automatically if it doesn't exist
- **Duplicate handling:** Append `_1`, `_2`, etc. if filename exists (e.g., `Screenshot_2025-12-06_14-30-00_1.png`)
- **No subfolder organization** for v0.1 (add date-based folders in v0.2 settings)

##### Auto-Start Implementation
- **Method:** Registry key `HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`
- **Key name:** `ScreenCaptureNET`
- **Value:** Full path to executable (e.g., `"C:\Apps\ScreenCapture.exe" --minimized`)
- **Toggle:** Enable/disable via tray context menu (v0.1), settings UI (v0.2)
- **No admin privileges required** (HKCU does not require elevation)
- **Startup argument:** `--minimized` flag to start in system tray without showing main window

### Out of Scope (v0.1)

- [ ] Cloud upload/sharing
- [ ] Online editor/web interface
- [ ] Similar image search
- [ ] Browser extensions
- [ ] Settings UI (hardcoded defaults for v0.1)
- [ ] Blur/pixelate tools
- [ ] Highlight tools
- [ ] Screenshot history
- [ ] Installer/auto-updates

---

## Architecture Overview

### High-Level Design

```
┌─────────────────────────────────────────────────────────┐
│                   Application Entry Point                │
│                    (App.xaml.cs Main)                    │
└──────────┬──────────────────────────────┬────────────────┘
           │                              │
           ▼                              ▼
    ┌──────────────────┐        ┌────────────────────┐
    │  System Tray     │        │ Hotkey Manager     │
    │    Monitor       │        │  (Global Listener) │
    └──────────────────┘        └─────────┬──────────┘
                                          │
                                   Press Print Screen
                                          │
                                          ▼
                        ┌──────────────────────────────┐
                        │   Selection Overlay Window   │
                        │  (Area Selection + Preview)  │
                        └──────────┬───────────────────┘
                                   │
                          User selects area
                                   │
                        ┌──────────▼───────────────────┐
                        │ Screenshot Capture Engine    │
                        │  (Bitmap generation)         │
                        └──────────┬───────────────────┘
                                   │
                        ┌──────────▼───────────────────┐
                        │  Quick Action Toolbar        │
                        │ (Save/Edit/Copy/Cancel)      │
                        └───┬──────────┬───────────┬───┘
                            │          │           │
                    ┌───────▼──┐  ┌───▼──┐  ┌────▼─────┐
                    │  Editor  │  │Save  │  │ Copy to  │
                    │ Window   │  │ File │  │ Clipboard│
                    └───────┬──┘  └──────┘  └──────────┘
                            │
            ┌───────────────┼────────────────┐
            ▼               ▼                ▼
    ┌─────────────┐  ┌────────────┐  ┌───────────────┐
    │Drawing Tools│  │Undo/Redo   │  │Color Manager  │
    │(Pencil,    │  │(Command     │  │(Palette,      │
    │Arrow, Rect,│  │Pattern)     │  │Custom RGB)    │
    │Text)       │  │             │  │               │
    └─────────────┘  └────────────┘  └───────────────┘
```

### Core Modules

| Module | Purpose | Key Classes |
|--------|---------|------------|
| **Capture** | Screenshot acquisition | `ScreenCaptureManager`, `SelectionOverlay` |
| **Editor** | Image annotation | `EditorWindow`, `AnnotationCanvas` |
| **Drawing** | Annotation commands | `DrawingCommand` hierarchy, `CommandHistory` |
| **Hotkeys** | Global hotkey registration | `GlobalHotkeyManager`, `NativeInterop` |
| **IO** | File operations | `ImageExporter`, `SettingsManager` |
| **UI** | User interface | `QuickActionToolbar`, `MainWindow` |
| **Models** | Data structures | `CaptureSettings`, `EditorSettings`, `Annotation` |

---

## Detailed Phase Breakdown

### Phase 1: Project Setup & Infrastructure (2-3 days)

**Objectives:**
- Initialize solution structure
- Configure build environment
- Set up Git repository
- Create base project scaffolding

**Deliverables:**
- [ ] Visual Studio 2022 solution file (.sln)
- [ ] Three class libraries:
  - `ScreenCapture.Core` (business logic)
  - `ScreenCapture.UI` (WPF UI)
  - `ScreenCapture.App` (entry point)
- [ ] NuGet packages installed and configured
- [ ] .gitignore configured
- [ ] README.md with setup instructions

**Key Tasks:**
1. Create solution with folder structure (see File Structure section)
2. Create App.xaml.cs with Main entry point
3. Add NuGet dependencies (listed below)
4. Configure project file for .NET 6+
5. Create initial Git commits (initial setup)

**Acceptance Criteria:**
- [ ] Solution builds without errors
- [ ] Application launches (empty window)
- [ ] Git repo initialized and structure documented

---

### Phase 2: Global Hotkey Registration (3-5 days)

**Objectives:**
- Implement Windows API P/Invoke for global hotkeys
- Create reusable hotkey manager
- Register Print Screen hotkey on app startup
- Handle hotkey events

**Deliverables:**
- [ ] `GlobalHotkeyManager.cs` - Main hotkey handler
- [ ] `NativeInterop.cs` - Windows API definitions
- [ ] `HotkeyEventArgs.cs` - Event arguments
- [ ] Unit tests for hotkey registration
- [ ] Integration with `MainWindow`

**Key Classes:**

```csharp
// GlobalHotkeyManager.cs
public class GlobalHotkeyManager : IDisposable
{
    public event EventHandler<HotkeyEventArgs> HotKeyPress;
    
    public bool RegisterHotKey(int hotkeyId, ModifierKey modifiers, VirtualKey key);
    public bool UnregisterHotKey(int hotkeyId);
    public void Dispose();
}

// NativeInterop.cs
public static class NativeInterop
{
    // P/Invoke declarations:
    // - RegisterHotKey
    // - UnregisterHotKey
    // - PostMessage
    // - Enums: ModifierKey, VirtualKey
}
```

**Acceptance Criteria:**
- [ ] Pressing Print Screen triggers hotkey event
- [ ] No errors in Event Log
- [ ] Manager properly cleans up on app exit
- [ ] Works with multiple instances (only latest gets hotkey)

---

### Phase 3: Selection Overlay Window (3-5 days)

**Objectives:**
- Create transparent overlay window
- Implement area selection with mouse events
- Display selection rectangle with handles
- Show dimensions in real-time
- Dim non-selected screen areas

**Deliverables:**
- [ ] `SelectionOverlay.xaml` - XAML UI definition
- [ ] `SelectionOverlay.xaml.cs` - Code-behind
- [ ] `SelectionViewModel.cs` - MVVM ViewModel
- [ ] Mouse event handlers (MouseMove, MouseDown, MouseUp)
- [ ] Resize handle detection & dragging
- [ ] Unit tests for selection logic

**Key Properties:**
- Full-screen semi-transparent overlay
- Selection box with 8 resize handles (corners + edges)
- Real-time dimension display (width × height)
- Keyboard controls:
  - **Escape** → Cancel selection
  - **Enter** → Confirm selection
- Mouse cursor feedback (resize arrows on handles)

**XAML Structure:**
```xaml
<Window>
  <!-- Background overlay (semi-transparent) -->
  <!-- Selection rectangle with handles -->
  <!-- Dimension text display -->
  <!-- Control buttons (OK, Cancel) -->
</Window>
```

**Acceptance Criteria:**
- [ ] Overlay appears full-screen with proper transparency
- [ ] Selection box follows mouse smoothly
- [ ] Resize handles work on all 8 points
- [ ] Dimensions update in real-time
- [ ] Escape key cancels selection
- [ ] Multi-monitor support verified

---

### Phase 4: Screenshot Capture Engine (3-5 days)

**Objectives:**
- Implement actual screen capture logic
- Support full screen, window, and area modes
- Handle multi-monitor capture
- Generate Bitmap from selected area
- Optimize performance

**Deliverables:**
- [ ] `ScreenCaptureManager.cs` - Main capture logic
- [ ] `IScreenCapture.cs` - Interface definition
- [ ] `ScreenCaptureException.cs` - Custom exception
- [ ] Unit tests for capture scenarios
- [ ] Performance profiling results

**Key Methods:**

```csharp
public class ScreenCaptureManager : IScreenCapture, IDisposable
{
    public Bitmap CaptureArea(Rectangle area);
    public Bitmap CaptureFullScreen();
    public Bitmap CaptureWindow(IntPtr windowHandle);
    public List<Monitor> GetAllMonitors();
    public void Dispose();
}
```

**Implementation Notes:**
- Use `System.Drawing.Graphics.CopyFromScreen()` for capture
- Handle DPI awareness for high-resolution displays
- Support multi-monitor virtual screen coordinates
- Cache screen information (refresh on demand)

**Acceptance Criteria:**
- [ ] CaptureArea works for arbitrary rectangles
- [ ] CaptureFullScreen includes all monitors
- [ ] CaptureWindow correctly captures window content
- [ ] DPI scaling handled correctly (4K monitors)
- [ ] Bitmap disposed properly after use
- [ ] No memory leaks in repeated captures

---

### Phase 5: Quick Action Toolbar (3-5 days)

**Objectives:**
- Create floating toolbar window
- Implement action buttons (Edit, Save, Copy, Cancel)
- Position toolbar near selection
- Connect to capture manager & editor

**Deliverables:**
- [ ] `QuickActionToolbar.xaml` - UI layout
- [ ] `QuickActionToolbar.xaml.cs` - Code-behind
- [ ] Button click handlers
- [ ] Integration with ScreenCaptureManager

**Layout:**
```
┌─────────────────────────────┐
│ [Edit] [Save] [Copy] [✕]   │
└─────────────────────────────┘
```

**Button Actions:**
- **Edit** → Open `EditorWindow` with captured bitmap
- **Save** → Save bitmap to disk (PNG, default location)
- **Copy** → Copy bitmap to Windows clipboard
- **Cancel (✕)** → Discard capture, close toolbar

**Technical Details:**
- Window style: `ToolWindow`, `TopMost`
- Position: Center of selected area (or near it)
- No resize/minimize buttons
- Keyboard shortcuts:
  - **E** → Edit
  - **S** → Save
  - **C** → Copy
  - **Escape** → Cancel

**Acceptance Criteria:**
- [ ] Toolbar appears after selection confirmed
- [ ] All buttons functional
- [ ] Toolbar closes after any action
- [ ] Toolbar appears on correct monitor (multi-monitor)
- [ ] Keyboard shortcuts work

---

### Phase 6: Editor Window - UI Framework (4-6 days)

**Objectives:**
- Create main editor window layout
- Design toolbar for tool selection
- Implement canvas for drawing
- Create color picker UI
- Layout action buttons (Save, Cancel)

**Deliverables:**
- [ ] `EditorWindow.xaml` - Main editor UI
- [ ] `EditorWindow.xaml.cs` - Code-behind
- [ ] `EditorViewModel.cs` - MVVM ViewModel
- [ ] `AnnotationCanvas.cs` - Custom drawing surface

**Window Layout:**
```
┌─────────────────────────────────────────┐
│ File  Edit  View  Help                  │
├─────────────────────────────────────────┤
│ [🎨] [✏️] [↗️] [□] [A]  | Size:[↓] Color:[●]
├──────────────────────────────────────────┤
│                                          │
│            DRAWING CANVAS                │ 
│         (with image + annotations)       │
│                                          │
│                                          │
├──────────────────────────────────────────┤
│ [↶ Undo] [↷ Redo]  [💾 Save] [✕ Cancel]│
└──────────────────────────────────────────┘
```

**Toolbar Items:**
- Tool selector buttons (Pencil, Arrow, Rectangle, Text)
- Size slider (3-20px for drawing, 8-48px for text)
- Color picker button
- Undo/Redo buttons

**Canvas:**
- Display original captured bitmap
- Overlay canvas for annotations
- Support zoom in/out (optional for v0.1)
- Mouse tracking for drawing

**Action Buttons:**
- **Undo** (Ctrl+Z)
- **Redo** (Ctrl+Y)
- **Save** (Ctrl+S) → Export dialog
- **Cancel** → Close without saving

**Acceptance Criteria:**
- [ ] Editor window launches with captured image
- [ ] All UI elements visible and aligned
- [ ] Tool buttons highlight when selected
- [ ] Color picker clickable
- [ ] Size slider functional
- [ ] Window resizable
- [ ] Image scales to fit window

---

### Phase 7: Drawing Engine - Core Tools (5-7 days)

**Objectives:**
- Implement drawing command architecture
- Create Pencil tool (freehand drawing)
- Create Arrow tool (line with arrowhead)
- Create Rectangle tool (hollow outline)
- Implement rendering pipeline

**Deliverables:**
- [ ] `DrawingCommand.cs` - Abstract base class
- [ ] `PencilCommand.cs` - Freehand drawing
- [ ] `ArrowCommand.cs` - Arrow/line tool
- [ ] `RectangleCommand.cs` - Rectangle tool
- [ ] `Annotation.cs` - Data model for annotations
- [ ] `AnnotationRenderer.cs` - Rendering logic
- [ ] Unit tests for drawing commands

**Command Pattern Architecture:**

```csharp
public abstract class DrawingCommand
{
    public abstract void Execute(DrawingContext context);
    public abstract void Undo(DrawingContext context);
    public Annotation ToAnnotation();
}

public class DrawingContext
{
    public Bitmap Canvas { get; set; }
    public Graphics GraphicsObject { get; set; }
    public Color CurrentColor { get; set; }
    public int CurrentSize { get; set; }
}
```

**Tool Implementations:**

**PencilCommand:**
- Store series of Point objects (polyline)
- Render with `Graphics.DrawLine()` between points
- Smooth curves using path interpolation

**ArrowCommand:**
- Store start point and end point
- Draw line using `Graphics.DrawLine()`
- Calculate arrowhead from angle and draw triangle

**RectangleCommand:**
- Store top-left and bottom-right points
- Draw hollow rectangle with `Graphics.DrawRectangle()`
- Support dynamic sizing while dragging

**Rendering:**
- Composite: Original image → Each annotation in order
- Use `DrawingVisual` for efficient rendering
- Re-render on every mouse move during drawing
- Final flatten to single Bitmap on save

**Acceptance Criteria:**
- [ ] Pencil draws smooth curves
- [ ] Arrow draws with proper arrowhead
- [ ] Rectangle draws hollow outline
- [ ] All tools respect color and size settings
- [ ] Real-time rendering during drawing
- [ ] No visual artifacts or flicker
- [ ] Performance acceptable on 4K images

---

### Phase 8: Text Tool & Color Management (4-5 days)

**Objectives:**
- Implement text annotation tool
- Create text input dialog/popup
- Build color picker UI
- Manage color palette (predefined + custom)

**Deliverables:**
- [ ] `TextCommand.cs` - Text annotation
- [ ] `TextInputWindow.xaml` - Text entry dialog
- [ ] `ColorManager.cs` - Color management
- [ ] `ColorPicker.xaml` - Color selection UI
- [ ] Predefined color palette

**Text Tool Implementation:**

```csharp
public class TextCommand : DrawingCommand
{
    public string Text { get; set; }
    public Point Position { get; set; }
    public int FontSize { get; set; }
    
    public override void Execute(DrawingContext context)
    {
        // Draw text on canvas using Graphics.DrawString()
    }
}
```

**Text Workflow:**
1. User clicks "Text" tool
2. Mouse cursor changes to text cursor (I-beam)
3. User clicks on canvas → TextInputWindow opens at click position
4. User types text → Press Enter to confirm
5. Text appears on canvas
6. Text can be edited (double-click to re-edit) or moved (click and drag)

**Text Tool UX Details:**
- **Default font:** Segoe UI (Windows system font)
- **Font selection:** Not available in v0.1 (add font picker in v0.2)
- **Text positioning:** Click to place initial position, then drag text box to reposition
- **Text editing:** Double-click existing text annotation to re-open edit dialog
- **Multi-line text:** Press **Shift+Enter** for new line within text, **Enter** to confirm and close
- **Text box appearance:** Show dotted border while editing, hide border after confirmation
- **Maximum text length:** 500 characters (prevent memory/rendering issues)
- **Empty text handling:** If user confirms with empty text, cancel the annotation (no empty text boxes)
- **Text selection:** Click once to select (shows resize handles), double-click to edit content

**Color Manager:**
- Predefined colors: Red, Blue, Green, Yellow, Black, White, Orange, Purple
- Custom color selection via RGB picker
- Recent colors palette (last 5 used)
- Current color indicator in toolbar

**Color Picker Persistence:**
- **Recent colors:** Persist in memory during session only (v0.1)
- **Custom colors:** Not saved across application restarts in v0.1
- **Selected color:** Persists for subsequent annotations within the same editing session
- **Default color on editor open:** Red (hardcoded for v0.1)
- **v0.2 enhancement:** Save recent/custom colors to `settings.json` file

**Drawing Size Units:**
- All sizes are specified in **device-independent pixels (DIP)**
- Visual size remains consistent regardless of monitor DPI settings
- **Drawing tools (Pencil, Arrow, Rectangle):** 3-20 DIP (default: 5 DIP)
- **Text tool font size:** 8-48 DIP (default: 16 DIP)
- Size slider shows actual DIP value with visual preview

**ColorPicker UI:**
```xaml
<ComboBox ItemsSource="{Binding PredefinedColors}" />
<Button Content="Custom..." Click="OpenCustomColorDialog" />
<Rectangle Fill="{Binding CurrentColor}" />
```

**Acceptance Criteria:**
- [ ] Text tool creates editable text annotations
- [ ] Font size adjustable (8-48px)
- [ ] Color picker shows predefined colors
- [ ] Custom RGB color selection works
- [ ] Color persists for next annotation
- [ ] Text renders at correct position and size
- [ ] Recent colors tracked and accessible

---

### Phase 9: Undo/Redo System (3-4 days)

**Objectives:**
- Implement command history stack
- Support unlimited undo/redo
- Update UI button states
- Handle edge cases

**Deliverables:**
- [ ] `CommandHistory.cs` - History manager
- [ ] `IUndoable.cs` - Interface for commands
- [ ] Unit tests for undo/redo scenarios
- [ ] Integration with EditorWindow

**CommandHistory Implementation:**

```csharp
public class CommandHistory
{
    private Stack<DrawingCommand> undoStack = new();
    private Stack<DrawingCommand> redoStack = new();
    
    public void Execute(DrawingCommand command)
    {
        command.Execute(context);
        undoStack.Push(command);
        redoStack.Clear(); // Clear redo stack on new command
    }
    
    public void Undo()
    {
        if (undoStack.Count > 0)
        {
            var command = undoStack.Pop();
            command.Undo(context);
            redoStack.Push(command);
        }
    }
    
    public void Redo()
    {
        if (redoStack.Count > 0)
        {
            var command = redoStack.Pop();
            command.Execute(context);
            undoStack.Push(command);
        }
    }
    
    public bool CanUndo => undoStack.Count > 0;
    public bool CanRedo => redoStack.Count > 0;
}
```

**UI Integration:**
- Undo button disabled when CanUndo = false
- Redo button disabled when CanRedo = false
- Keyboard shortcuts: Ctrl+Z (Undo), Ctrl+Y (Redo)
- Menu items: Edit > Undo, Edit > Redo

**Acceptance Criteria:**
- [ ] Undo removes last annotation
- [ ] Redo restores removed annotation
- [ ] Buttons disable when no history available
- [ ] Redo clears when new command executed after undo
- [ ] Works with all tool types
- [ ] Performance acceptable with 100+ annotations

---

### Phase 10: Save & Export (3-4 days)

**Objectives:**
- Implement image save functionality
- Support PNG and JPG formats
- Handle file dialogs
- Create quality settings
- Implement clipboard copy

**Deliverables:**
- [ ] `ImageExporter.cs` - Export logic
- [ ] Save dialog integration
- [ ] JPG quality settings
- [ ] Clipboard operations
- [ ] Unit tests

**ImageExporter Class:**

```csharp
public class ImageExporter
{
    public static void SaveAsPng(Bitmap image, string filePath);
    public static void SaveAsJpg(Bitmap image, string filePath, int quality = 90);
    public static void CopyToClipboard(Bitmap image);
    public static string GetDefaultSaveFolder();
}
```

**Save Workflow:**
1. User clicks "Save" button
2. SaveFileDialog opens with default location
3. User selects filename and format (.png or .jpg)
4. File saved to disk
5. Success notification shown
6. Editor window closes

**JPG Quality Settings:**
- Default: 90 (high quality)
- Adjustable via slider (60-100) in v0.2

**Clipboard Integration:**
- Use `System.Windows.Forms.Clipboard` (requires STAThread)
- Copy automatically after capture (optional)
- Copy button in editor window

**Acceptance Criteria:**
- [ ] SaveFileDialog appears with valid defaults
- [ ] PNG files save correctly (lossless)
- [ ] JPG files save correctly (lossy, quality adjustable)
- [ ] Copy to clipboard works
- [ ] File paths with special characters handled
- [ ] Overwrite confirmation shown if file exists
- [ ] No image quality loss with PNG

---

### Phase 11: System Tray Integration (2-3 days)

**Objectives:**
- Add system tray icon
- Implement minimize to tray
- Create context menu
- Handle app lifecycle

**Deliverables:**
- [ ] Tray icon and resources
- [ ] TrayIcon implementation
- [ ] Context menu (Open, Settings, Exit)
- [ ] MainWindow minimize behavior

**Tray Features:**
- Icon in system notification area
- Double-click to restore window
- Right-click context menu:
  - **New Screenshot** → Trigger capture
  - **Settings** → Open settings (v0.2)
  - **Exit** → Quit application
- Auto-minimize to tray on startup

**MainWindow Behavior:**
- Show/Hide on tray icon click
- Minimize to tray (not taskbar)
- Close button minimizes (not exits)
- Exit only via tray menu

**Acceptance Criteria:**
- [ ] Tray icon appears on startup
- [ ] Context menu works
- [ ] Double-click restores window
- [ ] New Screenshot works from tray
- [ ] App exits cleanly
- [ ] No lingering processes

---

### Phase 12: Testing & Bug Fixes (5-7 days)

**Objectives:**
- Execute comprehensive testing
- Fix bugs and edge cases
- Performance optimization
- Documentation completion

**Testing Strategy:**

**Unit Tests:**
- Test each DrawingCommand implementation
- Test CommandHistory undo/redo logic
- Test ScreenCaptureManager capture scenarios
- Test ImageExporter save/copy operations
- Test hotkey registration/cleanup

**Integration Tests:**
- Full capture → edit → save workflow
- Multi-window editor instances
- Tool switching and parameter changes
- Clipboard operations

**Manual Testing:**
- Multi-monitor capture verification
- High DPI (4K monitor) testing
- Windows 10/11 compatibility
- Edge cases:
  - Capture with taskbar
  - Capture with overlaying windows
  - Very large images (8K resolution)
  - Rapid tool switching
  - Editing very long text

**Performance Testing:**
- Memory usage during editing
- CPU usage during drawing
- Startup time
- Save/load times

**Deliverables:**
- [ ] Unit test project with 80%+ coverage
- [ ] Bug tracking spreadsheet
- [ ] Performance benchmarks
- [ ] Known issues documented
- [ ] Release notes

**Acceptance Criteria:**
- [ ] No critical bugs
- [ ] Performance acceptable (<100MB RAM)
- [ ] All test scenarios pass
- [ ] Documentation complete
- [ ] Ready for v0.1 release

---

## Technical Specifications

### System Requirements

**Minimum:**
- Windows 10 (Build 1909+)
- .NET 6.0 Runtime or .NET Framework 4.8+
- 512 MB RAM
- 50 MB disk space

**Recommended:**
- Windows 11
- .NET 6.0
- 2 GB RAM
- 100 MB disk space

### Performance Targets

| Metric | Target |
|--------|--------|
| Startup time | <2 seconds |
| Hotkey response | <100ms |
| Drawing responsiveness | 60 FPS (no lag) |
| Memory usage (idle) | <50 MB |
| Memory usage (editing 4K image) | <300 MB |
| Save operation (4K image) | <1 second |

### Compatibility

- ✅ Windows 10 (21H2+)
- ✅ Windows 11
- ✅ Single and multi-monitor setups
- ✅ High DPI displays (125%, 150%, 200%)
- ✅ Display scaling

### Threading Model

- **Main Thread:** UI operations only
- **Worker Threads:** 
  - Screen capture (background task)
  - File I/O (async operations)
  - Image processing (if needed in v0.2)

### Exception Handling

**Custom Exceptions:**
- `ScreenCaptureException` - Capture failures
- `ExportException` - Save/export failures
- `HotkeyException` - Hotkey registration failures

**Handling Strategy:**
- Log all exceptions
- Show user-friendly error messages
- Attempt graceful recovery
- Never silently fail

---

## Data Models

### CaptureSettings.cs

```csharp
public class CaptureSettings
{
    public VirtualKey HotKey { get; set; } = VirtualKey.PrintScreen;
    public ModifierKey HotKeyModifiers { get; set; } = ModifierKey.None;
    public string DefaultSaveFolder { get; set; } = 
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    public bool CopyToClipboardAfterCapture { get; set; } = true;
    public bool ShowPreviewBeforeSave { get; set; } = true;
    public int DefaultJpqQuality { get; set; } = 90;
    public bool StartMinimized { get; set; } = true;
    
    // Serialization
    public static CaptureSettings LoadFromFile(string path);
    public void SaveToFile(string path);
}
```

### EditorSettings.cs

```csharp
public class EditorSettings
{
    public int DefaultPenSize { get; set; } = 3;
    public int DefaultTextSize { get; set; } = 16;
    public Color DefaultColor { get; set; } = Colors.Red;
    public List<Color> RecentColors { get; set; } = new();
    public bool RememberWindowPosition { get; set; } = true;
    
    public static EditorSettings LoadFromFile(string path);
    public void SaveToFile(string path);
}
```

### Annotation.cs

```csharp
public abstract class Annotation
{
    public int Id { get; set; }
    public Color Color { get; set; }
    public int Size { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public abstract void Render(Graphics graphics);
    public abstract Annotation Clone();
}

public class PencilAnnotation : Annotation
{
    public List<Point> Points { get; set; }
    public override void Render(Graphics graphics) { /*...*/ }
}

public class ArrowAnnotation : Annotation
{
    public Point StartPoint { get; set; }
    public Point EndPoint { get; set; }
    public override void Render(Graphics graphics) { /*...*/ }
}

public class RectangleAnnotation : Annotation
{
    public Rectangle Bounds { get; set; }
    public override void Render(Graphics graphics) { /*...*/ }
}

public class TextAnnotation : Annotation
{
    public string Text { get; set; }
    public Point Position { get; set; }
    public int FontSize { get; set; }
    public override void Render(Graphics graphics) { /*...*/ }
}
```

---

## File Structure

```
ScreenCapture/
├── ScreenCapture.sln
├── .gitignore
├── README.md
│
├── ScreenCapture.Core/
│   ├── ScreenCapture.Core.csproj
│   ├── Capture/
│   │   ├── ScreenCaptureManager.cs
│   │   ├── SelectionOverlay.xaml
│   │   ├── SelectionOverlay.xaml.cs
│   │   ├── SelectionViewModel.cs
│   │   └── IScreenCapture.cs
│   ├── Editor/
│   │   ├── EditorWindow.xaml
│   │   ├── EditorWindow.xaml.cs
│   │   ├── EditorViewModel.cs
│   │   └── AnnotationCanvas.cs
│   ├── Drawing/
│   │   ├── DrawingCommand.cs
│   │   ├── CommandHistory.cs
│   │   ├── Annotations/
│   │   │   ├── Annotation.cs
│   │   │   ├── PencilAnnotation.cs
│   │   │   ├── ArrowAnnotation.cs
│   │   │   ├── RectangleAnnotation.cs
│   │   │   └── TextAnnotation.cs
│   │   ├── Commands/
│   │   │   ├── PencilCommand.cs
│   │   │   ├── ArrowCommand.cs
│   │   │   ├── RectangleCommand.cs
│   │   │   └── TextCommand.cs
│   │   └── AnnotationRenderer.cs
│   ├── Hotkeys/
│   │   ├── GlobalHotkeyManager.cs
│   │   ├── NativeInterop.cs
│   │   ├── HotkeyEventArgs.cs
│   │   └── VirtualKey.cs
│   ├── IO/
│   │   ├── ImageExporter.cs
│   │   ├── SettingsManager.cs
│   │   └── Logger.cs
│   ├── Models/
│   │   ├── CaptureSettings.cs
│   │   ├── EditorSettings.cs
│   │   ├── Monitor.cs
│   │   └── DrawingContext.cs
│   └── Extensions/
│       ├── BitmapExtensions.cs
│       └── ColorExtensions.cs
│
├── ScreenCapture.UI/
│   ├── ScreenCapture.UI.csproj
│   ├── QuickActionToolbar.xaml
│   ├── QuickActionToolbar.xaml.cs
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   ├── Resources/
│   │   ├── Icons/
│   │   │   ├── app-icon.ico
│   │   │   ├── tray-icon.ico
│   │   │   └── tool-icons/ (pencil, arrow, etc.)
│   │   └── Styles/
│   │       ├── Colors.xaml
│   │       └── Brushes.xaml
│   └── App.xaml
│
├── ScreenCapture.App/
│   ├── ScreenCapture.App.csproj
│   └── Program.cs (entry point)
│
├── ScreenCapture.Tests/
│   ├── ScreenCapture.Tests.csproj
│   ├── Capture/
│   │   ├── ScreenCaptureManagerTests.cs
│   │   └── SelectionOverlayTests.cs
│   ├── Drawing/
│   │   ├── CommandHistoryTests.cs
│   │   ├── PencilCommandTests.cs
│   │   ├── ArrowCommandTests.cs
│   │   └── RectangleCommandTests.cs
│   ├── Hotkeys/
│   │   └── GlobalHotkeyManagerTests.cs
│   ├── IO/
│   │   ├── ImageExporterTests.cs
│   │   └── SettingsManagerTests.cs
│   └── Integration/
│       ├── CaptureToSaveWorkflowTests.cs
│       └── EditorWorkflowTests.cs
│
└── docs/
    ├── ARCHITECTURE.md
    ├── API_REFERENCE.md
    ├── HOTKEY_IMPLEMENTATION.md
    └── TESTING_GUIDE.md
```

---

## Dependencies & NuGet Packages

### Required Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `System.Drawing.Common` | 7.0+ | Image handling, graphics |
| `System.Windows.Forms` | 4.7+ | Clipboard, file dialogs |
| `System.Management` | 7.0+ | System information |
| `xunit` | 2.4+ | Unit testing |
| `xunit.runner.visualstudio` | 2.4+ | Test runner |
| `Moq` | 4.16+ | Mocking for tests |

### Optional Packages (v0.2+)

| Package | Purpose |
|---------|---------|
| `SkiaSharp` | Advanced image processing (blur, pixelate) |
| `CommunityToolkit.Mvvm` | MVVM helpers |
| `Serilog` | Advanced logging |

### Package Reference Format

```xml
<ItemGroup>
    <PackageReference Include="System.Drawing.Common" Version="7.0.0" />
    <PackageReference Include="System.Windows.Forms" Version="4.7.0" />
    <PackageReference Include="xunit" Version="2.4.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.4.5" />
    <PackageReference Include="Moq" Version="4.16.1" />
</ItemGroup>
```

---

## Key Algorithms & Implementations

### 1. Global Hotkey Registration (Windows API)

**File:** `NativeInterop.cs`

```csharp
public static class NativeInterop
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    public const uint WM_HOTKEY = 0x0312;

    // Virtual key codes
    public const uint VK_PRINT = 0x2C;
    public const uint VK_A = 0x41;
    public const uint VK_S = 0x53;
    // ... more keys
}
```

### 2. Smooth Curve Drawing with Bezier Interpolation

**File:** `PencilCommand.cs`

```csharp
public class PencilCommand : DrawingCommand
{
    private List<Point> _points = new();

    public void AddPoint(Point p)
    {
        _points.Add(p);
    }

    public override void Execute(DrawingContext context)
    {
        if (_points.Count < 2) return;

        using (var pen = new Pen(Color, Size))
        {
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            pen.LineJoin = LineJoin.Round;

            // Draw line segments between points
            for (int i = 1; i < _points.Count; i++)
            {
                context.Graphics.DrawLine(pen, _points[i - 1], _points[i]);
            }
        }
    }
}
```

### 3. Arrow Tool with Arrowhead Calculation

**File:** `ArrowCommand.cs`

```csharp
public class ArrowCommand : DrawingCommand
{
    public Point StartPoint { get; set; }
    public Point EndPoint { get; set; }

    public override void Execute(DrawingContext context)
    {
        using (var pen = new Pen(Color, Size))
        {
            // Draw main line
            context.Graphics.DrawLine(pen, StartPoint, EndPoint);

            // Calculate arrowhead
            double angle = Math.Atan2(
                EndPoint.Y - StartPoint.Y,
                EndPoint.X - StartPoint.X);

            int arrowSize = Size * 3;
            Point[] arrowHead = new[]
            {
                EndPoint,
                new Point(
                    (int)(EndPoint.X - arrowSize * Math.Cos(angle - Math.PI / 6)),
                    (int)(EndPoint.Y - arrowSize * Math.Sin(angle - Math.PI / 6))),
                new Point(
                    (int)(EndPoint.X - arrowSize * Math.Cos(angle + Math.PI / 6)),
                    (int)(EndPoint.Y - arrowSize * Math.Sin(angle + Math.PI / 6)))
            };

            // Draw arrowhead (filled triangle)
            using (var brush = new SolidBrush(Color))
            {
                context.Graphics.FillPolygon(brush, arrowHead);
            }
        }
    }
}
```

### 4. Undo/Redo Stack Management

**File:** `CommandHistory.cs`

```csharp
public class CommandHistory
{
    private Stack<DrawingCommand> _undoStack = new();
    private Stack<DrawingCommand> _redoStack = new();
    private DrawingContext _context;

    public CommandHistory(DrawingContext context)
    {
        _context = context;
    }

    public void Execute(DrawingCommand command)
    {
        command.Execute(_context);
        _undoStack.Push(command);
        _redoStack.Clear(); // Clear redo when new command executed
    }

    public void Undo()
    {
        if (_undoStack.Count > 0)
        {
            var command = _undoStack.Pop();
            command.Undo(_context);
            _redoStack.Push(command);
            OnHistoryChanged?.Invoke();
        }
    }

    public void Redo()
    {
        if (_redoStack.Count > 0)
        {
            var command = _redoStack.Pop();
            command.Execute(_context);
            _undoStack.Push(command);
            OnHistoryChanged?.Invoke();
        }
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public event Action OnHistoryChanged;
}
```

### 5. Multi-Monitor Aware Screen Capture

**File:** `ScreenCaptureManager.cs`

```csharp
public Bitmap CaptureArea(Rectangle area)
{
    // Account for DPI scaling
    var dpiX = GetDpiForMonitor(area);
    var scaledArea = new Rectangle(
        (int)(area.X * dpiX),
        (int)(area.Y * dpiX),
        (int)(area.Width * dpiX),
        (int)(area.Height * dpiX));

    var bitmap = new Bitmap(scaledArea.Width, scaledArea.Height);
    using (var graphics = Graphics.FromImage(bitmap))
    {
        graphics.CopyFromScreen(scaledArea, Point.Empty, CopyPixelOperation.SourceCopy);
    }
    return bitmap;
}

public List<Monitor> GetAllMonitors()
{
    var monitors = new List<Monitor>();
    foreach (var screen in Screen.AllScreens)
    {
        monitors.Add(new Monitor
        {
            DeviceName = screen.DeviceName,
            Bounds = screen.Bounds,
            IsPrimary = screen.Primary,
            DPI = GetDpiForScreen(screen)
        });
    }
    return monitors;
}
```

---

## Testing Strategy

### Unit Testing Structure

**Test Project:** `ScreenCapture.Tests`

**Test Categories:**

1. **Capture Tests** (`Capture/`)
   - Test area capture with various rectangle dimensions
   - Test full screen capture on single/multi-monitor
   - Test DPI scaling
   - Test invalid inputs (negative dimensions, out of bounds)

2. **Drawing Tests** (`Drawing/`)
   - Test each annotation type renders correctly
   - Test CommandHistory undo/redo operations
   - Test annotation data persistence
   - Test drawing with various sizes/colors

3. **Hotkey Tests** (`Hotkeys/`)
   - Test hotkey registration succeeds
   - Test hotkey registration fails gracefully
   - Test cleanup on dispose

4. **IO Tests** (`IO/`)
   - Test save as PNG with various quality levels
   - Test save as JPG with quality settings
   - Test clipboard copy operation
   - Test file overwrite handling

5. **Integration Tests** (`Integration/`)
   - Full workflow: capture → edit → save
   - Tool switching during editing
   - Undo/redo across tool changes

### Test Naming Convention

```csharp
[Fact]
public void MethodName_WithCondition_ExpectedResult()
{
    // Arrange
    var input = new Rectangle(0, 0, 100, 100);

    // Act
    var result = captureManager.CaptureArea(input);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(100, result.Width);
}
```

### Code Coverage Goals

- **Overall:** 80%+ coverage
- **Core modules:** 85%+ (Capture, Drawing, IO)
- **UI layer:** 40%+ (harder to test, acceptable lower coverage)

---

## Known Challenges & Solutions

### Challenge 1: Multi-Monitor DPI Scaling

**Problem:**
Different monitors may have different DPI settings (100%, 125%, 150%, etc.). Screen coordinates don't account for DPI.

**Solution:**
- Use `GetDpiForMonitor()` Windows API to get monitor DPI
- Scale capture coordinates by DPI factor before capture
- Store DPI value and account for it in rendering

### Challenge 2: High-Performance Drawing

**Problem:**
Real-time drawing on large (4K) images may cause lag or flicker.

**Solution:**
- Use `DrawingVisual` instead of direct bitmap operations
- Implement double-buffering in WPF
- Batch graphics operations
- Consider region-based re-rendering (only redraw changed areas)

### Challenge 3: Hotkey Conflicts

**Problem:**
Another application may have already registered Print Screen hotkey.

**Solution:**
- Check RegisterHotKey return value
- Log failures with system error code
- Allow user to customize hotkey in settings (v0.2)
- Suggest alternative keys to user

### Challenge 4: Clipboard Thread Safety

**Problem:**
`System.Windows.Forms.Clipboard` requires STAThread context.

**Solution:**
- Mark Main() with `[STAThread]` attribute
- Wrap clipboard operations in try-catch
- Handle "Clipboard is locked" exceptions gracefully

### Challenge 5: Memory Management with Large Images

**Problem:**
Editing large (8K) images may consume significant memory.

**Solution:**
- Dispose Bitmap objects immediately after use
- Use `using` statements consistently
- Implement image downsampling for preview (full resolution on save)
- Monitor memory usage in testing

---

## Keyboard Shortcuts Reference

### Global (System-wide)

| Shortcut | Action |
|----------|--------|
| Print Screen | Start capture (open selection overlay) |
| Ctrl+Print Screen | Capture entire screen immediately |
| Alt+Print Screen | Capture active window immediately |

### Selection Overlay

| Shortcut | Action |
|----------|--------|
| Escape | Cancel selection and close overlay |
| Enter | Confirm selection |
| Arrow Keys | Move selection by 1 pixel |
| Shift + Arrow Keys | Move selection by 10 pixels |
| Alt | Toggle window selection mode |
| Ctrl + A | Select entire screen |

### Quick Action Toolbar

| Shortcut | Action |
|----------|--------|
| E | Open editor |
| S | Save to default location |
| C | Copy to clipboard |
| Escape | Cancel and discard capture |

### Editor Window

| Shortcut | Action |
|----------|--------|
| Ctrl + Z | Undo last action |
| Ctrl + Y | Redo last undone action |
| Ctrl + S | Save image (opens save dialog) |
| Ctrl + Shift + S | Quick save to default location |
| Ctrl + C | Copy image to clipboard |
| Escape | Cancel and close editor (with confirmation if unsaved changes) |
| 1 | Select Pencil tool |
| 2 | Select Arrow tool |
| 3 | Select Rectangle tool |
| 4 | Select Text tool |
| [ | Decrease brush/font size |
| ] | Increase brush/font size |
| Delete | Delete selected annotation |

### Text Editing Mode

| Shortcut | Action |
|----------|--------|
| Enter | Confirm text and close editor |
| Shift + Enter | Insert new line |
| Escape | Cancel text editing |

---

## Error Handling & User Messages

### Error Display Strategy

- **Method:** Windows Toast Notifications (non-blocking) for minor errors
- **Fallback:** Modal dialog for critical errors that require user action
- **Position:** Toast appears near system tray; dialogs appear centered on active monitor
- **Duration:** Toast notifications auto-dismiss after 5 seconds

### User-Facing Error Messages

| Scenario | Message | Type |
|----------|---------|------|
| Capture failed | "Unable to capture screen. Please try again." | Toast |
| Hotkey already registered | "Print Screen is in use by another application. Please close conflicting apps or use Ctrl+Print Screen." | Dialog |
| Hotkey registration failed | "Could not register hotkey. Try running the application as administrator." | Dialog |
| Save failed (permissions) | "Cannot save to this location. Please choose a different folder." | Dialog |
| Save failed (disk full) | "Not enough disk space. Please free up space and try again." | Dialog |
| Save failed (invalid path) | "Invalid file path. Please choose a valid location." | Dialog |
| Clipboard failed | "Could not copy to clipboard. Another application may be using it. Please try again." | Toast |
| Clipboard locked | "Clipboard is currently in use. Retrying..." | Toast (auto-retry 3 times) |
| Image too large | "Image is too large to process. Please capture a smaller area." | Dialog |
| Out of memory | "Not enough memory to complete this operation. Please close other applications." | Dialog |
| File already exists | "File already exists. Overwrite?" | Dialog (Yes/No/Cancel) |
| Unsaved changes | "You have unsaved changes. Save before closing?" | Dialog (Save/Don't Save/Cancel) |

### Error Logging

- All errors logged to `%APPDATA%\ScreenCapture.NET\logs\error.log`
- Log format: `[YYYY-MM-DD HH:mm:ss] [ERROR] [Component] Message - StackTrace`
- Logs rotate daily, keep last 7 days

---

## Deliverables Checklist

### Code Deliverables

- [ ] **ScreenCapture.Core** project with all core modules
- [ ] **ScreenCapture.UI** project with WPF UI components
- [ ] **ScreenCapture.App** console entry point
- [ ] **ScreenCapture.Tests** with unit and integration tests
- [ ] All classes match specifications in this document
- [ ] Code follows C# naming conventions (PascalCase for public, camelCase for private)
- [ ] Comments/XML documentation for public APIs
- [ ] No TODO comments or unfinished code

### Documentation

- [ ] **README.md** - Installation and quick start
- [ ] **ARCHITECTURE.md** - System design overview
- [ ] **API_REFERENCE.md** - Public API documentation
- [ ] **HOTKEY_IMPLEMENTATION.md** - Windows API details
- [ ] **TESTING_GUIDE.md** - How to run tests
- [ ] **CHANGELOG.md** - Version history

### Testing

- [ ] Unit test project created
- [ ] 80%+ code coverage achieved
- [ ] All tests passing
- [ ] Integration tests for full workflows
- [ ] Manual testing completed on Windows 10/11
- [ ] Multi-monitor testing completed
- [ ] High DPI testing completed (4K monitor)

### Build & Release

- [ ] Solution builds without warnings
- [ ] Release build configuration (x64)
- [ ] No dependencies on debug-only packages
- [ ] Portable executable (no installer for v0.1)
- [ ] Application icon and resources included
- [ ] Release notes documenting v0.1 features

### Performance Verification

- [ ] Startup time < 2 seconds
- [ ] Hotkey response < 100ms
- [ ] Drawing at 60 FPS (no lag)
- [ ] Memory usage < 50MB idle
- [ ] 4K image editing < 300MB RAM
- [ ] Save operation < 1 second

---

## Version 0.2 Roadmap

### New Features

- [ ] **Blur/Pixelate Tool** - Redact sensitive information
- [ ] **Highlight Tool** - Semi-transparent marker for emphasis
- [ ] **Eraser Tool** - Remove annotations selectively
- [ ] **Settings Window** - Hotkey customization, default folders, file formats
- [ ] **Brush Styles** - Dotted, dashed, solid line options
- [ ] **Opacity Slider** - Adjustable transparency for all tools
- [ ] **Screenshot History** - Recent captures accessible from main window
- [ ] **Crop Tool** - Post-capture image cropping
- [ ] **Drag & Drop** - Open images for annotation
- [ ] **Undo/Redo Visualization** - Visual history panel

### Enhancements

- [ ] **Customizable Hotkeys** - GUI for changing hotkey bindings
- [ ] **Dark Mode** - Theme support
- [ ] **Multi-language** - Dutch, English, French support
- [ ] **Auto-save** - Periodic backup of edits
- [ ] **Image Templates** - Common frame sizes (phone, tablet, social media)
- [ ] **Keyboard Shortcuts** - Comprehensive shortcut reference
- [ ] **Resize/Rotate** - Post-capture transformations

### Polish

- [ ] **Installer** - NSIS or WiX setup package
- [ ] **Auto-updates** - Check for updates on startup
- [ ] **Uninstaller** - Clean removal from Windows
- [ ] **Portable ZIP** - Standalone executable distribution

---

## Success Criteria for v0.1 Release

✅ **Functional Completeness:**
- All features in scope implemented and tested
- Zero critical bugs
- All unit tests passing

✅ **Performance:**
- Meets performance targets (startup <2s, 60 FPS drawing)
- Memory usage within limits
- No crashes or hangs

✅ **Usability:**
- Intuitive workflow (hotkey → capture → edit → save)
- Clear UI with labeled buttons
- Smooth interaction (no lag or freezing)

✅ **Reliability:**
- Handles edge cases gracefully
- Proper error messages for failures
- Doesn't corrupt system (no admin required)

✅ **Documentation:**
- README with installation steps
- API documentation complete
- Technical architecture documented

---



**Document Version:** 1.0  
**Last Updated:** December 6, 2025  
**Status:** Ready for Development  
**Prepared by:** AI Assistant  
**For:** C# Developer - LightShot Clone Project