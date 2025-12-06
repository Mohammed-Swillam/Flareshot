# LightShot Clone - Detailed Implementation Task List (v0.1 MVP)

## Project Overview

This implementation plan breaks down the LightShot clone development into logical, ordered sections with specific tasks. The app follows the Lightshot UX pattern where annotation happens directly on the overlay (not a separate editor window), with tool panel on the right and action bar at the bottom.

**Key Architecture Decisions:**
- Move all XAML to `ScreenCapture.UI` and keep `ScreenCapture.Core` as pure business logic
- Use `CommunityToolkit.Mvvm` from the start for cleaner code
- Use WPF `DrawingVisual` for rendering (better hardware acceleration)
- Target .NET 8.0
- Enforce single instance
- Annotation happens directly on overlay (not separate editor window)

---

## Section 1: Project Setup & Infrastructure

### 1.1 Solution & Project Structure
- [ ] Create Visual Studio solution `ScreenCapture.sln`
- [ ] Create `ScreenCapture.Core` class library (.NET 8.0) - pure business logic, no XAML
- [ ] Create `ScreenCapture.UI` WPF Application (.NET 8.0) - all XAML/UI
- [ ] Create `ScreenCapture.Tests` xUnit test project (.NET 8.0)
- [ ] Configure project references (UI → Core, Tests → Core)
- [ ] Add `.gitignore` for .NET/Visual Studio
- [ ] Create initial `README.md` with project description

### 1.2 NuGet Dependencies
- [ ] Add `CommunityToolkit.Mvvm` to Core and UI projects
- [ ] Add `System.Drawing.Common` (7.0+) to Core
- [ ] Add `xunit`, `xunit.runner.visualstudio`, `Moq` to Tests project
- [ ] Configure `<UseWindowsForms>true</UseWindowsForms>` in UI project for clipboard/dialogs

### 1.3 Application Entry Point
- [ ] Configure single-instance enforcement using Mutex
- [ ] Add `[STAThread]` attribute to Main
- [ ] Implement `--minimized` startup argument handling
- [ ] Create `App.xaml` with application resources
- [ ] Set application icon

### 1.4 Base Infrastructure
- [ ] Create folder structure in Core: `Capture/`, `Drawing/`, `Hotkeys/`, `IO/`, `Models/`, `Services/`
- [ ] Create folder structure in UI: `Views/`, `ViewModels/`, `Controls/`, `Resources/`, `Converters/`
- [ ] Create base `ViewModelBase` class using `ObservableObject`
- [ ] Create `RelayCommand` wrappers if needed (or use CommunityToolkit's built-in)

---

## Section 2: Settings & Configuration

### 2.1 Settings Model
- [ ] Create `AppSettings` class with properties:
  - Hotkey combination (key + modifiers)
  - Default save folder path
  - Default image format (PNG/JPG)
  - JPG quality (60-100)
  - Copy to clipboard after capture (bool)
  - Start minimized (bool)
  - Auto-start with Windows (bool)
- [ ] Implement JSON serialization/deserialization
- [ ] Create `SettingsManager` service for load/save operations
- [ ] Define default settings values

### 2.2 Settings Storage
- [ ] Create settings file path: `%APPDATA%\ScreenCapture.NET\settings.json`
- [ ] Implement auto-create directory if not exists
- [ ] Handle corrupted settings file (reset to defaults)
- [ ] Implement settings change notification

### 2.3 Settings Window UI
- [ ] Create `SettingsWindow.xaml` with sections:
  - **General**: Start minimized, auto-start with Windows
  - **Capture**: Hotkey configuration (key picker + modifier checkboxes)
  - **Save**: Default folder (with browse button), default format, JPG quality slider
  - **Behavior**: Copy to clipboard after capture
- [ ] Create `SettingsViewModel` with two-way bindings
- [ ] Implement Save/Cancel/Apply buttons
- [ ] Add hotkey recording functionality (press key to set)
- [ ] Validate hotkey conflicts

---

## Section 3: Global Hotkey System

### 3.1 Windows API Interop
- [ ] Create `NativeInterop.cs` with P/Invoke declarations:
  - `RegisterHotKey`
  - `UnregisterHotKey`
  - Window message constants (`WM_HOTKEY`)
- [ ] Define `ModifierKeys` enum (Alt, Ctrl, Shift, Win)
- [ ] Define `VirtualKey` enum with common keys

### 3.2 Hotkey Manager
- [ ] Create `GlobalHotkeyManager` class implementing `IDisposable`
- [ ] Implement hotkey registration with error handling
- [ ] Implement hotkey unregistration
- [ ] Create `HotkeyPressed` event with `HotkeyEventArgs`
- [ ] Handle registration failure gracefully (show user message)
- [ ] Support re-registration when hotkey changes in settings

### 3.3 Integration with App Lifecycle
- [ ] Register hotkey on application startup
- [ ] Unregister hotkey on application shutdown
- [ ] Handle hotkey event to trigger capture overlay
- [ ] Re-register when user changes hotkey in settings

---

## Section 4: System Tray Integration

### 4.1 Tray Icon Setup
- [ ] Create tray icon resource (`.ico` file)
- [ ] Implement `NotifyIcon` wrapper for WPF
- [ ] Show tray icon on application start
- [ ] Handle tray icon double-click (show main window or trigger capture)

### 4.2 Tray Context Menu
- [ ] Create context menu with items:
  - **New Screenshot** - trigger capture
  - **Settings** - open settings window
  - **Separator**
  - **Auto-start with Windows** (checkbox toggle)
  - **Separator**
  - **Exit** - quit application
- [ ] Wire up menu item commands
- [ ] Implement auto-start registry toggle (`HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`)

### 4.3 Application Lifecycle
- [ ] Minimize to tray instead of taskbar
- [ ] Close button minimizes to tray (not exit)
- [ ] Exit only via tray menu "Exit" option
- [ ] Clean disposal of tray icon on exit

---

## Section 5: Screen Capture Engine

### 5.1 Monitor Detection
- [ ] Create `MonitorInfo` model (bounds, device name, DPI, isPrimary)
- [ ] Implement `GetAllMonitors()` using `Screen.AllScreens`
- [ ] Calculate virtual screen bounds (union of all monitors)
- [ ] Handle DPI scaling per monitor

### 5.2 Screen Capture Manager
- [ ] Create `IScreenCaptureService` interface
- [ ] Implement `ScreenCaptureService` class
- [ ] Implement `CaptureVirtualScreen()` - capture all monitors as one bitmap
- [ ] Implement `CaptureArea(Rectangle area)` - capture specific region
- [ ] Implement `CaptureWindow(IntPtr hwnd)` - capture specific window
- [ ] Handle DPI-aware coordinate conversion
- [ ] Proper bitmap disposal pattern

### 5.3 Window Detection (for window capture mode)
- [ ] Create P/Invoke for `EnumWindows`, `GetWindowRect`, `GetWindowText`
- [ ] Implement `GetVisibleWindows()` returning list of window info
- [ ] Filter out invisible/minimized windows
- [ ] Implement `GetWindowAtPoint(Point pt)` for hover detection

---

## Section 6: Selection Overlay Window

### 6.1 Overlay Window Setup
- [ ] Create `OverlayWindow.xaml` as full-screen borderless window
- [ ] Set `WindowStyle="None"`, `AllowsTransparency="True"`, `Topmost="True"`
- [ ] Span overlay across all monitors (virtual screen bounds)
- [ ] Set background to semi-transparent black (50% opacity)
- [ ] Handle ESC key to cancel and close overlay

### 6.2 Selection Logic
- [ ] Track mouse events: MouseDown, MouseMove, MouseUp
- [ ] Implement selection rectangle drawing (start point → current point)
- [ ] Enforce minimum selection size (10×10 pixels)
- [ ] Show crosshair cursor during initial selection
- [ ] Display selection dimensions (e.g., "346×420") near selection

### 6.3 Selection Rectangle Rendering
- [ ] Render clear/transparent area for selected region (shows actual screen)
- [ ] Keep dimmed overlay on non-selected areas
- [ ] Draw dashed border (alternating blue/white) around selection
- [ ] Draw 8 resize handles (corners + edge midpoints, 8px squares)

### 6.4 Selection Manipulation
- [ ] Implement resize via handles (8 directions)
- [ ] Implement move by dragging inside selection
- [ ] Arrow keys: move selection by 1px
- [ ] Shift+Arrow keys: move selection by 10px
- [ ] Enter key: confirm selection (stay on overlay for annotation)
- [ ] Escape key: cancel entire capture

### 6.5 Window Selection Mode
- [ ] Toggle window selection mode with Alt key
- [ ] Highlight window under cursor with colored border
- [ ] Click to select window bounds as selection rectangle
- [ ] Handle edge cases (minimized, off-screen windows)

---

## Section 7: Annotation Tool Panel (Right Side)

### 7.1 Tool Panel UI
- [ ] Create `ToolPanel` UserControl
- [ ] Vertical layout with tool buttons (icon-based)
- [ ] Tools: Pencil, Line, Arrow, Rectangle, Marker/Highlighter, Text
- [ ] Color picker button (shows current color)
- [ ] Undo button
- [ ] Visual feedback for selected tool (highlight/border)

### 7.2 Tool Panel Positioning
- [ ] Position panel to the RIGHT of selection by default
- [ ] If not enough space on right → position on LEFT
- [ ] If not enough space on sides → position on TOP or BOTTOM
- [ ] Ensure panel stays within screen bounds
- [ ] Update position when selection is resized/moved

### 7.3 Tool Selection State
- [ ] Create `SelectedTool` enum (Pencil, Line, Arrow, Rectangle, Marker, Text, None)
- [ ] Track currently selected tool in ViewModel
- [ ] Update cursor based on selected tool
- [ ] Keyboard shortcuts: 1=Pencil, 2=Line, 3=Arrow, 4=Rectangle, 5=Marker, 6=Text

---

## Section 8: Drawing Engine (WPF DrawingVisual)

### 8.1 Drawing Infrastructure
- [ ] Create `AnnotationCanvas` custom control extending `FrameworkElement`
- [ ] Override `OnRender` for efficient drawing
- [ ] Implement hit-testing for annotation selection
- [ ] Handle mouse events for drawing operations

### 8.2 Annotation Models
- [ ] Create abstract `Annotation` base class:
  - `Id`, `Color`, `StrokeWidth`, `CreatedAt`
  - Abstract `Render(DrawingContext dc)` method
- [ ] Create `PencilAnnotation` (list of points, polyline)
- [ ] Create `LineAnnotation` (start point, end point)
- [ ] Create `ArrowAnnotation` (start point, end point, arrowhead)
- [ ] Create `RectangleAnnotation` (bounds rectangle, hollow)
- [ ] Create `MarkerAnnotation` (list of points, semi-transparent wide stroke)
- [ ] Create `TextAnnotation` (position, text content, font size)

### 8.3 Drawing Commands (for Undo/Redo)
- [ ] Create `IDrawingCommand` interface with `Execute()` and `Undo()`
- [ ] Create command implementations for each annotation type
- [ ] Commands add/remove annotations from canvas

### 8.4 Real-Time Drawing
- [ ] Handle MouseDown: start new annotation
- [ ] Handle MouseMove: update annotation preview (live drawing)
- [ ] Handle MouseUp: finalize annotation, push to command history
- [ ] Render all completed annotations + current preview

---

## Section 9: Individual Drawing Tools

### 9.1 Pencil Tool
- [ ] Capture mouse points on MouseMove
- [ ] Render as polyline with round caps/joins
- [ ] Smooth curve rendering between points

### 9.2 Line Tool
- [ ] Store start point on MouseDown
- [ ] Preview line to current mouse position
- [ ] Finalize on MouseUp

### 9.3 Arrow Tool
- [ ] Same as Line Tool for the shaft
- [ ] Calculate arrowhead triangle from line angle
- [ ] Render filled arrowhead at end point

### 9.4 Rectangle Tool
- [ ] Store corner on MouseDown
- [ ] Preview rectangle to current mouse position
- [ ] Render hollow rectangle (stroke only, no fill)

### 9.5 Marker/Highlighter Tool
- [ ] Similar to Pencil but with:
  - Semi-transparent color (e.g., 50% opacity)
  - Wider stroke width
- [ ] Render with blend mode for highlighter effect

### 9.6 Text Tool
- [ ] Click to place text insertion point
- [ ] Show text input popup/inline textbox
- [ ] Support multi-line (Shift+Enter)
- [ ] Enter confirms text
- [ ] Escape cancels text entry
- [ ] Render text with specified font size and color

---

## Section 10: Color Picker & Settings

### 10.1 Color Picker UI
- [ ] Create `ColorPickerPopup` UserControl
- [ ] Show predefined colors grid: Red, Blue, Green, Yellow, Orange, Purple, Black, White
- [ ] Custom color button → opens Windows color dialog
- [ ] Recent colors row (last 5 used)
- [ ] Current color indicator

### 10.2 Brush Size Adjustment
- [ ] Size slider or +/- buttons (3-20px for drawing, 8-48px for text)
- [ ] Keyboard shortcuts: `[` decrease, `]` increase
- [ ] Visual preview of current size

### 10.3 Tool Settings State
- [ ] Track current color in ViewModel
- [ ] Track current stroke width
- [ ] Track current font size (for text tool)
- [ ] Persist within session (reset on new capture)

---

## Section 11: Undo/Redo System

### 11.1 Command History
- [ ] Create `CommandHistory` class with undo/redo stacks
- [ ] Implement `Execute(command)` - execute and push to undo stack
- [ ] Implement `Undo()` - pop from undo, execute undo, push to redo
- [ ] Implement `Redo()` - pop from redo, execute, push to undo
- [ ] Clear redo stack when new command executed

### 11.2 UI Integration
- [ ] Undo button in tool panel
- [ ] Keyboard shortcut: Ctrl+Z for undo, Ctrl+Y for redo
- [ ] Disable undo button when nothing to undo
- [ ] (Optional) redo button or rely on keyboard only

---

## Section 12: Action Bar (Bottom)

### 12.1 Action Bar UI
- [ ] Create `ActionBar` UserControl
- [ ] Horizontal layout with action buttons (icon + optional label)
- [ ] Actions: Copy, Save, Cancel/Discard
- [ ] (Future/v0.2: Upload, Share, Print, Search - out of scope for v0.1)

### 12.2 Action Bar Positioning
- [ ] Position BELOW selection by default
- [ ] If not enough space below → position ABOVE
- [ ] Center horizontally relative to selection
- [ ] Ensure bar stays within screen bounds

### 12.3 Action Implementations

#### 12.3.1 Copy to Clipboard
- [ ] Flatten annotations onto captured bitmap
- [ ] Copy result to Windows clipboard
- [ ] Show success feedback (brief visual indicator)
- [ ] Close overlay after copy

#### 12.3.2 Save to File
- [ ] Flatten annotations onto captured bitmap
- [ ] Show SaveFileDialog with default path/filename
- [ ] Default filename: `Screenshot_YYYY-MM-DD_HH-mm-ss.png`
- [ ] Support PNG and JPG formats
- [ ] Handle duplicate filenames (append `_1`, `_2`, etc.)
- [ ] Show success/failure feedback
- [ ] Close overlay after save

#### 12.3.3 Cancel/Discard
- [ ] Prompt if there are unsaved annotations (optional for v0.1)
- [ ] Close overlay without saving
- [ ] Return to normal desktop

---

## Section 13: Image Export

### 13.1 Image Flattening
- [ ] Create `ImageExporter` service
- [ ] Render captured region as base bitmap
- [ ] Render all annotations on top
- [ ] Return final composited bitmap

### 13.2 Save Operations
- [ ] Implement `SaveAsPng(bitmap, path)`
- [ ] Implement `SaveAsJpg(bitmap, path, quality)`
- [ ] Create default save folder if not exists
- [ ] Handle file system errors gracefully

### 13.3 Clipboard Operations
- [ ] Implement `CopyToClipboard(bitmap)`
- [ ] Handle clipboard locked scenarios (retry logic)
- [ ] Proper bitmap format for clipboard

---

## Section 14: Error Handling & Logging

### 14.1 Custom Exceptions
- [ ] Create `ScreenCaptureException`
- [ ] Create `HotkeyException`
- [ ] Create `ExportException`

### 14.2 Logging
- [ ] Create simple file logger
- [ ] Log to `%APPDATA%\ScreenCapture.NET\logs\`
- [ ] Log format: `[timestamp] [level] [component] message`
- [ ] Log errors with stack traces

### 14.3 User-Facing Error Messages
- [ ] Implement toast notification helper
- [ ] Show friendly error messages (not technical details)
- [ ] Handle critical errors with modal dialogs

---

## Section 15: Testing

### 15.1 Unit Tests - Core
- [ ] Test `CommandHistory` undo/redo logic
- [ ] Test `SettingsManager` load/save
- [ ] Test `ImageExporter` save operations
- [ ] Test annotation model serialization

### 15.2 Unit Tests - Hotkeys
- [ ] Test hotkey registration (mock-based)
- [ ] Test hotkey event firing

### 15.3 Integration Tests
- [ ] Test capture → annotate → save workflow
- [ ] Test settings persistence

### 15.4 Manual Testing Checklist
- [ ] Single monitor capture
- [ ] Multi-monitor capture
- [ ] High DPI (4K) capture
- [ ] All annotation tools
- [ ] Undo/redo functionality
- [ ] Save as PNG/JPG
- [ ] Copy to clipboard
- [ ] Settings persistence
- [ ] Auto-start toggle
- [ ] Hotkey registration

---

## Section 16: Polish & Final Tasks

### 16.1 UI Polish
- [ ] Create consistent icon set for tools/actions
- [ ] Apply consistent styling (colors, fonts, spacing)
- [ ] Ensure keyboard navigation works
- [ ] Add tooltips to all buttons

### 16.2 Documentation
- [ ] Update `README.md` with usage instructions
- [ ] Document keyboard shortcuts
- [ ] Add build instructions

### 16.3 Build & Release
- [ ] Configure Release build (x64)
- [ ] Ensure single portable EXE output
- [ ] Test on clean Windows 10/11 machine
- [ ] Create `CHANGELOG.md` for v0.1

---

## Summary

**Total: 16 Sections, ~150+ individual tasks**

This implementation plan provides a clear, ordered path from project setup to final release. Each section builds upon the previous ones, ensuring a logical development flow. The plan is designed to be executed sequentially, though some sections (like testing) can be developed in parallel with their corresponding features.

**Key Features for v0.1:**
- Global hotkey activation
- Area selection with visual feedback
- Annotation tools (pencil, line, arrow, rectangle, marker, text)
- Color picker and size adjustment
- Undo/redo functionality
- Save to file (PNG/JPG) and copy to clipboard
- System tray integration
- Settings window
- Single instance enforcement

Ready to proceed with implementation when you are. Which section would you like to start with?
