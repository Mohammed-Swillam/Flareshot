# Manual Testing Checklist - ScreenCapture.NET v0.1

## Pre-Test Setup
- [ ] Clean build: `dotnet build -c Release`
- [ ] Remove any previous settings: Delete `%APPDATA%\ScreenCapture.NET\` folder
- [ ] Close any running instances

---

## 1. Application Startup

### 1.1 Normal Startup
- [ ] Application starts without errors
- [ ] System tray icon appears
- [ ] Main window is visible (if not using --minimized)

### 1.2 Minimized Startup
- [ ] Run with `--minimized` argument
- [ ] Main window is hidden
- [ ] System tray icon is visible
- [ ] Tray notification shows "Running in system tray"

### 1.3 Debug Mode
- [ ] Run with `--debug` argument
- [ ] Check logs at `%APPDATA%\ScreenCapture.NET\logs\`
- [ ] Log file contains startup messages

### 1.4 Single Instance
- [ ] Start application
- [ ] Try to start second instance
- [ ] "Already running" message appears
- [ ] Only one instance exists in system tray

---

## 2. Screen Capture

### 2.1 Hotkey Activation
- [ ] Press Print Screen (default hotkey)
- [ ] Overlay appears covering entire screen
- [ ] Screen dims with semi-transparent overlay
- [ ] Instruction panel visible at top

### 2.2 Area Selection
- [ ] Click and drag to create selection
- [ ] Selection rectangle appears with blue dashed border
- [ ] Dimensions display shows current size
- [ ] Selection can be created in any direction (left-to-right, right-to-left, etc.)

### 2.3 Selection Adjustment
- [ ] Resize handles appear on selection corners/edges
- [ ] Drag corner handles to resize diagonally
- [ ] Drag edge handles to resize horizontally/vertically
- [ ] Click inside selection and drag to move it
- [ ] Minimum size enforced (10x10 pixels)

### 2.4 Keyboard Navigation
- [ ] Press ESC to cancel selection and close overlay
- [ ] Press Enter to confirm selection (copies to clipboard)
- [ ] Ctrl+C to confirm and copy

---

## 3. Annotation Tools

### 3.1 Tool Switching
- [ ] Annotation toolbar appears after selection
- [ ] Click each tool button to select it
- [ ] Keyboard shortcuts work: P (Pencil), L (Line), A (Arrow), R (Rectangle), M (Marker), T (Text)

### 3.2 Pencil Tool
- [ ] Select pencil tool
- [ ] Click and drag to draw freehand
- [ ] Drawing appears in current color
- [ ] Multiple strokes can be drawn

### 3.3 Line Tool
- [ ] Select line tool
- [ ] Click and drag to draw straight line
- [ ] Line preview shows during drag
- [ ] Line finalizes on mouse up

### 3.4 Arrow Tool
- [ ] Select arrow tool
- [ ] Click and drag to draw arrow
- [ ] Arrow head appears at end point
- [ ] Arrow is properly oriented

### 3.5 Rectangle Tool
- [ ] Select rectangle tool
- [ ] Click and drag to draw rectangle
- [ ] Rectangle is hollow (stroke only)
- [ ] Rectangle can be drawn in any direction

### 3.6 Marker Tool
- [ ] Select marker tool
- [ ] Click and drag to highlight
- [ ] Marker is semi-transparent
- [ ] Marker has wider stroke than pencil

### 3.7 Text Tool
- [ ] Select text tool
- [ ] Click to place text insertion point
- [ ] Text input box appears
- [ ] Type text and press Enter to confirm
- [ ] Shift+Enter for new line
- [ ] ESC to cancel text entry
- [ ] Text appears in current color

---

## 4. Color & Undo/Redo

### 4.1 Color Selection
- [ ] Current color displayed in toolbar
- [ ] Click color button to open color picker
- [ ] Select new color from picker
- [ ] Color button updates to show new color
- [ ] New drawings use selected color

### 4.2 Undo
- [ ] Draw something
- [ ] Click Undo button
- [ ] Last drawing is removed
- [ ] Ctrl+Z keyboard shortcut works

### 4.3 Redo
- [ ] Undo an action
- [ ] Click Redo button
- [ ] Undone action is restored
- [ ] Ctrl+Y keyboard shortcut works

### 4.4 Undo/Redo State
- [ ] Undo button disabled when nothing to undo
- [ ] Redo button disabled when nothing to redo
- [ ] New action clears redo stack

---

## 5. Save & Export

### 5.1 Copy to Clipboard
- [ ] Create selection with annotations
- [ ] Click Copy button (or press Enter/Ctrl+C)
- [ ] "Copied to Clipboard" notification appears
- [ ] Paste in another application (Paint, Word, etc.)
- [ ] Image includes annotations

### 5.2 Save to File
- [ ] Create selection with annotations
- [ ] Click Save button (or press Ctrl+S)
- [ ] Save dialog appears
- [ ] Default filename is `Screenshot_YYYY-MM-DD_HH-mm-ss.png`
- [ ] Default folder is Pictures\Screenshots
- [ ] Save as PNG
- [ ] Save as JPEG
- [ ] "Screenshot Saved" notification appears
- [ ] Click notification to open folder
- [ ] Saved file includes annotations

---

## 6. Action Toolbar

### 6.1 Close Button
- [ ] Click X button
- [ ] Overlay closes without saving

### 6.2 Button Tooltips
- [ ] Hover over each button
- [ ] Tooltip displays action name

---

## 7. System Tray

### 7.1 Tray Menu
- [ ] Right-click tray icon
- [ ] Menu appears with: New Screenshot, Settings, Auto-start toggle, Exit
- [ ] "New Screenshot" triggers capture
- [ ] "Settings" opens settings window
- [ ] "Exit" closes application

### 7.2 Tray Icon Double-Click
- [ ] Double-click tray icon
- [ ] Main window appears

---

## 8. Settings

### 8.1 Settings Window
- [ ] Open Settings from tray menu or main window
- [ ] Window displays current settings

### 8.2 Hotkey Configuration
- [ ] Click in hotkey field
- [ ] Press new key combination
- [ ] Hotkey display updates
- [ ] Save settings
- [ ] New hotkey works for capture

### 8.3 Behavior Settings
- [ ] Toggle "Start minimized"
- [ ] Toggle "Copy to clipboard after capture"
- [ ] Toggle "Start with Windows"
- [ ] Settings persist after restart

### 8.4 Save Settings
- [ ] "Default Save Folder" setting visible
- [ ] "JPEG Quality" slider works
- [ ] Settings are saved on Apply/OK

---

## 9. Multi-Monitor (if applicable)

### 9.1 Multi-Monitor Capture
- [ ] Overlay covers all monitors
- [ ] Selection can span across monitors
- [ ] Captured area is correct

---

## 10. Edge Cases

### 10.1 Small Selection
- [ ] Try to create selection smaller than 10x10
- [ ] Minimum size is enforced

### 10.2 Screen Edge Selection
- [ ] Create selection at screen edges
- [ ] Toolbars reposition to stay visible

### 10.3 Cancel During Drawing
- [ ] Start drawing annotation
- [ ] Press ESC
- [ ] Overlay closes, drawing is discarded

---

## Test Results

| Category | Pass | Fail | Notes |
|----------|------|------|-------|
| Startup | | | |
| Capture | | | |
| Annotations | | | |
| Color/Undo | | | |
| Save/Export | | | |
| Action Bar | | | |
| System Tray | | | |
| Settings | | | |
| Multi-Monitor | | | |
| Edge Cases | | | |

**Tester:** ________________
**Date:** ________________
**Version:** v0.1
