using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ScreenCapture.Core.Capture;
using ScreenCapture.Core.Drawing;
using ScreenCapture.UI.Controls;

using Rect = System.Windows.Rect;
using WinPoint = System.Windows.Point;
using DrawingRectangle = System.Drawing.Rectangle;
using Rectangle = System.Windows.Shapes.Rectangle;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;
using Brushes = System.Windows.Media.Brushes;

namespace ScreenCapture.UI.Views;

/// <summary>
/// Overlay window for selecting screen region and drawing annotations.
/// </summary>
public partial class OverlayWindow : Window
{
    private readonly IScreenCaptureService _captureService;
    private BitmapSource? _screenshotBackground;

    // Selection state
    private bool _isSelecting;
    private bool _hasSelection;
    private bool _isInEditMode;
    private WinPoint _startPoint;
    private WinPoint _currentPoint;
    private Rect _selectionRect;

    // Resize/move state
    private bool _isResizing;
    private bool _isMoving;
    private ResizeHandle _activeHandle;
    private WinPoint _moveStartPoint;
    private Rect _originalRect;

    // Drawing state
    private bool _isDrawing;
    private Annotation? _currentAnnotation;
    private readonly CommandHistory _commandHistory = new();
    private AnnotationTool _currentTool = AnnotationTool.None;
    private Color _currentColor = Colors.Red;

    // Visual elements
    private Rectangle? _dimOverlayTop;
    private Rectangle? _dimOverlayBottom;
    private Rectangle? _dimOverlayLeft;
    private Rectangle? _dimOverlayRight;
    private Rectangle? _selectionBorder;
    private readonly List<Rectangle> _resizeHandles = new();

    // Constants
    private const double MinSelectionSize = 10;
    private const double HandleSize = 8;
    private static readonly SolidColorBrush DimBrush = new(Color.FromArgb(128, 0, 0, 0));
    private static readonly SolidColorBrush SelectionBorderBrush = new(Color.FromRgb(33, 150, 243));
    private static readonly SolidColorBrush HandleBrush = new(Color.FromRgb(33, 150, 243));

    /// <summary>
    /// Event raised when selection is confirmed.
    /// </summary>
    public event EventHandler<SelectionConfirmedEventArgs>? SelectionConfirmed;

    /// <summary>
    /// Event raised when selection is cancelled.
    /// </summary>
    public event EventHandler? SelectionCancelled;

    /// <summary>
    /// Gets the selected region in screen coordinates.
    /// </summary>
    public DrawingRectangle SelectedRegion => new(
        (int)(_selectionRect.X + Left),
        (int)(_selectionRect.Y + Top),
        (int)_selectionRect.Width,
        (int)_selectionRect.Height);

    /// <summary>
    /// Gets the captured screenshot bitmap.
    /// </summary>
    public BitmapSource? ScreenshotBitmap => _screenshotBackground;

    /// <summary>
    /// Gets the drawing canvas for rendering annotations.
    /// </summary>
    public DrawingCanvas AnnotationCanvas => DrawingCanvas;

    public OverlayWindow(IScreenCaptureService captureService)
    {
        InitializeComponent();
        _captureService = captureService;

        // Wire up toolbar events
        AnnotationToolbar.ToolChanged += OnToolChanged;
        AnnotationToolbar.ColorChanged += OnColorChanged;
        AnnotationToolbar.UndoRequested += OnUndoRequested;
        AnnotationToolbar.RedoRequested += OnRedoRequested;

        ActionToolbar.CopyRequested += OnCopyRequested;
        ActionToolbar.SaveRequested += OnSaveRequested;
        ActionToolbar.CloseRequested += OnCloseRequested;

        // Wire up command history
        _commandHistory.HistoryChanged += OnHistoryChanged;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Position window to cover all screens
        var bounds = _captureService.GetVirtualScreenBounds();
        Left = bounds.Left;
        Top = bounds.Top;
        Width = bounds.Width;
        Height = bounds.Height;

        // Capture the screen
        CaptureScreen();

        // Create dim overlay elements
        CreateDimOverlays();

        // Focus to receive keyboard input
        Focus();
    }

    private void CaptureScreen()
    {
        try
        {
            using var bitmap = _captureService.CaptureVirtualScreen();
            _screenshotBackground = ConvertToBitmapSource(bitmap);

            // Set as canvas background
            var imageBrush = new ImageBrush(_screenshotBackground)
            {
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            };
            SelectionCanvas.Background = imageBrush;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to capture screen: {ex.Message}");
        }
    }

    private static BitmapSource ConvertToBitmapSource(System.Drawing.Bitmap bitmap)
    {
        var bitmapData = bitmap.LockBits(
            new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
            System.Drawing.Imaging.ImageLockMode.ReadOnly,
            bitmap.PixelFormat);

        try
        {
            var bitmapSource = BitmapSource.Create(
                bitmapData.Width,
                bitmapData.Height,
                96, 96,
                PixelFormats.Bgra32,
                null,
                bitmapData.Scan0,
                bitmapData.Stride * bitmapData.Height,
                bitmapData.Stride);

            bitmapSource.Freeze();
            return bitmapSource;
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
    }

    #region Edit Mode

    private void EnterEditMode()
    {
        if (_isInEditMode) return;

        _isInEditMode = true;

        // Show drawing canvas with clipping to selection area
        DrawingCanvas.Visibility = Visibility.Visible;
        DrawingCanvas.Width = ActualWidth;
        DrawingCanvas.Height = ActualHeight;
        DrawingCanvas.IsHitTestVisible = true;

        // Position and show toolbars
        PositionToolbars();
        AnnotationToolbar.Visibility = Visibility.Visible;
        ActionToolbar.Visibility = Visibility.Visible;

        // Switch cursor
        Cursor = Cursors.Arrow;
    }

    private void PositionToolbars()
    {
        // Position annotation toolbar to the right of selection
        double annotationX = _selectionRect.Right + 10;
        double annotationY = _selectionRect.Top;

        // Keep within screen bounds
        if (annotationX + 60 > ActualWidth)
        {
            annotationX = _selectionRect.Left - 60;
        }

        Canvas.SetLeft(AnnotationToolbar, annotationX);
        Canvas.SetTop(AnnotationToolbar, annotationY);

        // Position action toolbar below selection
        double actionX = _selectionRect.Left;
        double actionY = _selectionRect.Bottom + 10;

        // Keep within screen bounds
        if (actionY + 50 > ActualHeight)
        {
            actionY = _selectionRect.Top - 50;
        }

        Canvas.SetLeft(ActionToolbar, actionX);
        Canvas.SetTop(ActionToolbar, actionY);
    }

    #endregion

    #region Toolbar Event Handlers

    private void OnToolChanged(object? sender, AnnotationTool tool)
    {
        _currentTool = tool;

        // Update cursor based on tool
        if (tool == AnnotationTool.None)
        {
            Cursor = Cursors.Arrow;
        }
        else if (tool == AnnotationTool.Text)
        {
            Cursor = Cursors.IBeam;
        }
        else
        {
            Cursor = Cursors.Cross;
        }
    }

    private void OnColorChanged(object? sender, Color color)
    {
        _currentColor = color;
    }

    private void OnHistoryChanged(object? sender, EventArgs e)
    {
        AnnotationToolbar.UpdateUndoRedoState(_commandHistory.CanUndo, _commandHistory.CanRedo);
    }

    private void OnUndoRequested(object? sender, EventArgs e)
    {
        _commandHistory.Undo();
        DrawingCanvas.InvalidateVisual();
    }

    private void OnRedoRequested(object? sender, EventArgs e)
    {
        _commandHistory.Redo();
        DrawingCanvas.InvalidateVisual();
    }

    private void OnCopyRequested(object? sender, EventArgs e)
    {
        ConfirmSelection();
    }

    private void OnSaveRequested(object? sender, EventArgs e)
    {
        // TODO: Implement save to file in Section 9
        System.Windows.MessageBox.Show("Save functionality coming in next section!", "Coming Soon");
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Cancel();
    }

    #endregion

    #region Dim Overlays

    private void CreateDimOverlays()
    {
        _dimOverlayTop = CreateDimRect();
        _dimOverlayBottom = CreateDimRect();
        _dimOverlayLeft = CreateDimRect();
        _dimOverlayRight = CreateDimRect();

        SelectionCanvas.Children.Add(_dimOverlayTop);
        SelectionCanvas.Children.Add(_dimOverlayBottom);
        SelectionCanvas.Children.Add(_dimOverlayLeft);
        SelectionCanvas.Children.Add(_dimOverlayRight);

        // Initially cover entire screen
        UpdateDimOverlays(new Rect(0, 0, 0, 0));
    }

    private static Rectangle CreateDimRect()
    {
        return new Rectangle
        {
            Fill = DimBrush,
            IsHitTestVisible = false
        };
    }

    private void UpdateDimOverlays(Rect selection)
    {
        if (_dimOverlayTop == null) return;

        double canvasWidth = SelectionCanvas.ActualWidth;
        double canvasHeight = SelectionCanvas.ActualHeight;

        // Top overlay
        Canvas.SetLeft(_dimOverlayTop, 0);
        Canvas.SetTop(_dimOverlayTop, 0);
        _dimOverlayTop.Width = canvasWidth;
        _dimOverlayTop.Height = Math.Max(0, selection.Top);

        // Bottom overlay
        Canvas.SetLeft(_dimOverlayBottom, 0);
        Canvas.SetTop(_dimOverlayBottom, selection.Bottom);
        _dimOverlayBottom.Width = canvasWidth;
        _dimOverlayBottom.Height = Math.Max(0, canvasHeight - selection.Bottom);

        // Left overlay
        Canvas.SetLeft(_dimOverlayLeft, 0);
        Canvas.SetTop(_dimOverlayLeft, selection.Top);
        _dimOverlayLeft.Width = Math.Max(0, selection.Left);
        _dimOverlayLeft.Height = selection.Height;

        // Right overlay
        Canvas.SetLeft(_dimOverlayRight, selection.Right);
        Canvas.SetTop(_dimOverlayRight, selection.Top);
        _dimOverlayRight.Width = Math.Max(0, canvasWidth - selection.Right);
        _dimOverlayRight.Height = selection.Height;
    }

    #endregion

    #region Mouse Handling

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var point = e.GetPosition(SelectionCanvas);

        // If in edit mode with a tool selected, start drawing
        if (_isInEditMode && _currentTool != AnnotationTool.None)
        {
            // Check if point is within selection
            if (_selectionRect.Contains(point))
            {
                StartDrawing(point);
                SelectionCanvas.CaptureMouse();
                return;
            }
        }

        // Check if clicking on resize handle
        var handle = GetResizeHandleAtPoint(point);
        if (handle != ResizeHandle.None)
        {
            _isResizing = true;
            _activeHandle = handle;
            _originalRect = _selectionRect;
            _moveStartPoint = point;
            SelectionCanvas.CaptureMouse();
            return;
        }

        // Check if clicking inside selection (to move)
        if (_hasSelection && _selectionRect.Contains(point))
        {
            _isMoving = true;
            _moveStartPoint = point;
            _originalRect = _selectionRect;
            Cursor = Cursors.SizeAll;
            SelectionCanvas.CaptureMouse();
            return;
        }

        // Start new selection
        _isSelecting = true;
        _hasSelection = false;
        _isInEditMode = false;
        _startPoint = point;
        _currentPoint = point;

        // Hide toolbars and reset
        AnnotationToolbar.Visibility = Visibility.Collapsed;
        ActionToolbar.Visibility = Visibility.Collapsed;
        DrawingCanvas.Visibility = Visibility.Collapsed;
        DrawingCanvas.Clear();
        _commandHistory.Clear();

        // Hide instruction panel
        InstructionPanel.Visibility = Visibility.Collapsed;

        // Remove old selection visuals
        RemoveSelectionVisuals();

        SelectionCanvas.CaptureMouse();
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        var point = e.GetPosition(SelectionCanvas);

        if (_isDrawing)
        {
            ContinueDrawing(point);
        }
        else if (_isSelecting)
        {
            _currentPoint = point;
            UpdateSelection();
        }
        else if (_isResizing)
        {
            HandleResize(point);
        }
        else if (_isMoving)
        {
            HandleMove(point);
        }
        else if (_hasSelection && !_isInEditMode)
        {
            // Update cursor based on position
            UpdateCursor(point);
        }
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        SelectionCanvas.ReleaseMouseCapture();

        if (_isDrawing)
        {
            FinishDrawing();
        }
        else if (_isSelecting)
        {
            _isSelecting = false;
            
            // Check if selection is large enough
            if (_selectionRect.Width >= MinSelectionSize && _selectionRect.Height >= MinSelectionSize)
            {
                _hasSelection = true;
                CreateResizeHandles();
                EnterEditMode();
            }
            else
            {
                // Selection too small, reset
                _hasSelection = false;
                UpdateDimOverlays(new Rect(0, 0, 0, 0));
                InstructionPanel.Visibility = Visibility.Visible;
                RemoveSelectionVisuals();
            }
        }
        else if (_isResizing)
        {
            _isResizing = false;
            _activeHandle = ResizeHandle.None;
            UpdateResizeHandles();
            PositionToolbars();
        }
        else if (_isMoving)
        {
            _isMoving = false;
            Cursor = _currentTool == AnnotationTool.None ? Cursors.Arrow : Cursors.Cross;
            UpdateResizeHandles();
            PositionToolbars();
        }
    }

    #endregion

    #region Drawing

    private void StartDrawing(WinPoint point)
    {
        _isDrawing = true;

        switch (_currentTool)
        {
            case AnnotationTool.Pencil:
                var pencil = new PencilAnnotation { Color = _currentColor, IsPreview = true };
                pencil.Points.Add(point);
                _currentAnnotation = pencil;
                break;

            case AnnotationTool.Line:
                _currentAnnotation = new LineAnnotation
                {
                    StartPoint = point,
                    EndPoint = point,
                    Color = _currentColor,
                    IsPreview = true
                };
                break;

            case AnnotationTool.Arrow:
                _currentAnnotation = new ArrowAnnotation
                {
                    StartPoint = point,
                    EndPoint = point,
                    Color = _currentColor,
                    IsPreview = true
                };
                break;

            case AnnotationTool.Rectangle:
                _currentAnnotation = new RectangleAnnotation
                {
                    StartPoint = point,
                    EndPoint = point,
                    Color = _currentColor,
                    IsPreview = true
                };
                break;

            case AnnotationTool.Marker:
                var marker = new MarkerAnnotation { Color = _currentColor, IsPreview = true };
                marker.Points.Add(point);
                _currentAnnotation = marker;
                break;

            case AnnotationTool.Text:
                _isDrawing = false;
                ShowTextInput(point);
                return;
        }

        if (_currentAnnotation != null)
        {
            DrawingCanvas.SetPreview(_currentAnnotation);
        }
    }

    private void ContinueDrawing(WinPoint point)
    {
        if (_currentAnnotation == null) return;

        // Clamp to selection area
        point = new WinPoint(
            Math.Max(_selectionRect.Left, Math.Min(_selectionRect.Right, point.X)),
            Math.Max(_selectionRect.Top, Math.Min(_selectionRect.Bottom, point.Y)));

        switch (_currentAnnotation)
        {
            case PencilAnnotation pencil:
                pencil.Points.Add(point);
                break;

            case LineAnnotation line:
                line.EndPoint = point;
                break;

            case ArrowAnnotation arrow:
                arrow.EndPoint = point;
                break;

            case RectangleAnnotation rect:
                rect.EndPoint = point;
                break;

            case MarkerAnnotation marker:
                marker.Points.Add(point);
                break;
        }

        DrawingCanvas.SetPreview(_currentAnnotation);
    }

    private void FinishDrawing()
    {
        _isDrawing = false;

        if (_currentAnnotation != null)
        {
            _currentAnnotation.IsPreview = false;
            DrawingCanvas.SetPreview(null);

            // Add to history and canvas
            var command = new AddAnnotationCommand(DrawingCanvas, _currentAnnotation);
            _commandHistory.Execute(command);
        }

        _currentAnnotation = null;
    }

    #endregion

    #region Text Input

    private WinPoint _textInsertPoint;

    private void ShowTextInput(WinPoint point)
    {
        _textInsertPoint = point;

        Canvas.SetLeft(TextInputPanel, point.X);
        Canvas.SetTop(TextInputPanel, point.Y);
        TextInputPanel.Visibility = Visibility.Visible;

        TextInputBox.Text = "";
        TextInputBox.Focus();
    }

    private void TextInputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            CommitTextAnnotation();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            TextInputPanel.Visibility = Visibility.Collapsed;
            e.Handled = true;
        }
    }

    private void TextInputBox_LostFocus(object sender, RoutedEventArgs e)
    {
        CommitTextAnnotation();
    }

    private void CommitTextAnnotation()
    {
        TextInputPanel.Visibility = Visibility.Collapsed;

        var text = TextInputBox.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        var annotation = new TextAnnotation
        {
            Position = _textInsertPoint,
            Text = text,
            Color = _currentColor
        };

        var command = new AddAnnotationCommand(DrawingCanvas, annotation);
        _commandHistory.Execute(command);
    }

    #endregion

    #region Selection Updates

    private void UpdateSelection()
    {
        // Calculate selection rectangle (handle any drag direction)
        double x = Math.Min(_startPoint.X, _currentPoint.X);
        double y = Math.Min(_startPoint.Y, _currentPoint.Y);
        double width = Math.Abs(_currentPoint.X - _startPoint.X);
        double height = Math.Abs(_currentPoint.Y - _startPoint.Y);

        // Clamp to canvas bounds
        x = Math.Max(0, x);
        y = Math.Max(0, y);
        width = Math.Min(width, SelectionCanvas.ActualWidth - x);
        height = Math.Min(height, SelectionCanvas.ActualHeight - y);

        _selectionRect = new Rect(x, y, width, height);

        // Update dim overlays
        UpdateDimOverlays(_selectionRect);

        // Update or create selection border
        UpdateSelectionBorder();

        // Update dimensions display
        UpdateDimensionsDisplay();
    }

    private void UpdateSelectionBorder()
    {
        if (_selectionBorder == null)
        {
            _selectionBorder = new Rectangle
            {
                Stroke = SelectionBorderBrush,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 4 },
                Fill = Brushes.Transparent,
                IsHitTestVisible = false
            };
            SelectionCanvas.Children.Add(_selectionBorder);
        }

        Canvas.SetLeft(_selectionBorder, _selectionRect.X);
        Canvas.SetTop(_selectionBorder, _selectionRect.Y);
        _selectionBorder.Width = _selectionRect.Width;
        _selectionBorder.Height = _selectionRect.Height;
    }

    private void UpdateDimensionsDisplay()
    {
        DimensionsText.Text = $"{(int)_selectionRect.Width} × {(int)_selectionRect.Height}";

        // Position near selection (top-left, or above if too close to top)
        double x = _selectionRect.X;
        double y = _selectionRect.Y - 30;

        if (y < 10)
        {
            y = _selectionRect.Bottom + 5;
        }

        Canvas.SetLeft(DimensionsPanel, x);
        Canvas.SetTop(DimensionsPanel, y);
        DimensionsPanel.Visibility = Visibility.Visible;
    }

    private void RemoveSelectionVisuals()
    {
        if (_selectionBorder != null)
        {
            SelectionCanvas.Children.Remove(_selectionBorder);
            _selectionBorder = null;
        }

        foreach (var handle in _resizeHandles)
        {
            SelectionCanvas.Children.Remove(handle);
        }
        _resizeHandles.Clear();

        DimensionsPanel.Visibility = Visibility.Collapsed;
    }

    #endregion

    #region Resize Handles

    private enum ResizeHandle
    {
        None,
        TopLeft, Top, TopRight,
        Left, Right,
        BottomLeft, Bottom, BottomRight
    }

    private void CreateResizeHandles()
    {
        // Remove existing handles
        foreach (var handle in _resizeHandles)
        {
            SelectionCanvas.Children.Remove(handle);
        }
        _resizeHandles.Clear();

        // Create 8 handles
        for (int i = 0; i < 8; i++)
        {
            var handle = new Rectangle
            {
                Width = HandleSize,
                Height = HandleSize,
                Fill = HandleBrush,
                Stroke = Brushes.White,
                StrokeThickness = 1,
                IsHitTestVisible = false
            };
            _resizeHandles.Add(handle);
            SelectionCanvas.Children.Add(handle);
        }

        UpdateResizeHandles();
    }

    private void UpdateResizeHandles()
    {
        if (_resizeHandles.Count != 8) return;

        double offset = HandleSize / 2;
        double x = _selectionRect.X;
        double y = _selectionRect.Y;
        double w = _selectionRect.Width;
        double h = _selectionRect.Height;

        // Position handles: TL, T, TR, L, R, BL, B, BR
        var positions = new[]
        {
            (x - offset, y - offset),                       // TopLeft
            (x + w / 2 - offset, y - offset),               // Top
            (x + w - offset, y - offset),                   // TopRight
            (x - offset, y + h / 2 - offset),               // Left
            (x + w - offset, y + h / 2 - offset),           // Right
            (x - offset, y + h - offset),                   // BottomLeft
            (x + w / 2 - offset, y + h - offset),           // Bottom
            (x + w - offset, y + h - offset)                // BottomRight
        };

        for (int i = 0; i < 8; i++)
        {
            Canvas.SetLeft(_resizeHandles[i], positions[i].Item1);
            Canvas.SetTop(_resizeHandles[i], positions[i].Item2);
        }
    }

    private ResizeHandle GetResizeHandleAtPoint(WinPoint point)
    {
        if (_resizeHandles.Count != 8) return ResizeHandle.None;

        var handles = new[]
        {
            ResizeHandle.TopLeft, ResizeHandle.Top, ResizeHandle.TopRight,
            ResizeHandle.Left, ResizeHandle.Right,
            ResizeHandle.BottomLeft, ResizeHandle.Bottom, ResizeHandle.BottomRight
        };

        double hitSize = HandleSize + 4; // Slightly larger hit area

        for (int i = 0; i < 8; i++)
        {
            double hx = Canvas.GetLeft(_resizeHandles[i]);
            double hy = Canvas.GetTop(_resizeHandles[i]);
            var handleRect = new Rect(hx - 2, hy - 2, hitSize, hitSize);

            if (handleRect.Contains(point))
            {
                return handles[i];
            }
        }

        return ResizeHandle.None;
    }

    private void HandleResize(WinPoint point)
    {
        double dx = point.X - _moveStartPoint.X;
        double dy = point.Y - _moveStartPoint.Y;

        double x = _originalRect.X;
        double y = _originalRect.Y;
        double w = _originalRect.Width;
        double h = _originalRect.Height;

        switch (_activeHandle)
        {
            case ResizeHandle.TopLeft:
                x += dx; y += dy; w -= dx; h -= dy;
                break;
            case ResizeHandle.Top:
                y += dy; h -= dy;
                break;
            case ResizeHandle.TopRight:
                y += dy; w += dx; h -= dy;
                break;
            case ResizeHandle.Left:
                x += dx; w -= dx;
                break;
            case ResizeHandle.Right:
                w += dx;
                break;
            case ResizeHandle.BottomLeft:
                x += dx; w -= dx; h += dy;
                break;
            case ResizeHandle.Bottom:
                h += dy;
                break;
            case ResizeHandle.BottomRight:
                w += dx; h += dy;
                break;
        }

        // Ensure minimum size
        if (w < MinSelectionSize)
        {
            if (_activeHandle == ResizeHandle.Left || _activeHandle == ResizeHandle.TopLeft || _activeHandle == ResizeHandle.BottomLeft)
                x = _originalRect.Right - MinSelectionSize;
            w = MinSelectionSize;
        }
        if (h < MinSelectionSize)
        {
            if (_activeHandle == ResizeHandle.Top || _activeHandle == ResizeHandle.TopLeft || _activeHandle == ResizeHandle.TopRight)
                y = _originalRect.Bottom - MinSelectionSize;
            h = MinSelectionSize;
        }

        // Clamp to canvas
        x = Math.Max(0, x);
        y = Math.Max(0, y);
        w = Math.Min(w, SelectionCanvas.ActualWidth - x);
        h = Math.Min(h, SelectionCanvas.ActualHeight - y);

        _selectionRect = new Rect(x, y, w, h);
        UpdateDimOverlays(_selectionRect);
        UpdateSelectionBorder();
        UpdateDimensionsDisplay();
        UpdateResizeHandles();
    }

    private void HandleMove(WinPoint point)
    {
        double dx = point.X - _moveStartPoint.X;
        double dy = point.Y - _moveStartPoint.Y;

        double x = _originalRect.X + dx;
        double y = _originalRect.Y + dy;

        // Clamp to canvas
        x = Math.Max(0, Math.Min(x, SelectionCanvas.ActualWidth - _selectionRect.Width));
        y = Math.Max(0, Math.Min(y, SelectionCanvas.ActualHeight - _selectionRect.Height));

        _selectionRect = new Rect(x, y, _selectionRect.Width, _selectionRect.Height);
        UpdateDimOverlays(_selectionRect);
        UpdateSelectionBorder();
        UpdateDimensionsDisplay();
        UpdateResizeHandles();
    }

    private void UpdateCursor(WinPoint point)
    {
        var handle = GetResizeHandleAtPoint(point);
        
        Cursor = handle switch
        {
            ResizeHandle.TopLeft or ResizeHandle.BottomRight => Cursors.SizeNWSE,
            ResizeHandle.TopRight or ResizeHandle.BottomLeft => Cursors.SizeNESW,
            ResizeHandle.Top or ResizeHandle.Bottom => Cursors.SizeNS,
            ResizeHandle.Left or ResizeHandle.Right => Cursors.SizeWE,
            ResizeHandle.None when _selectionRect.Contains(point) => Cursors.SizeAll,
            _ => Cursors.Cross
        };
    }

    #endregion

    #region Keyboard Handling

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        // Handle tool shortcuts
        if (_isInEditMode)
        {
            switch (e.Key)
            {
                case Key.P:
                    AnnotationToolbar.SelectTool(AnnotationTool.Pencil);
                    e.Handled = true;
                    return;
                case Key.L:
                    AnnotationToolbar.SelectTool(AnnotationTool.Line);
                    e.Handled = true;
                    return;
                case Key.A:
                    if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    {
                        AnnotationToolbar.SelectTool(AnnotationTool.Arrow);
                        e.Handled = true;
                        return;
                    }
                    break;
                case Key.R:
                    AnnotationToolbar.SelectTool(AnnotationTool.Rectangle);
                    e.Handled = true;
                    return;
                case Key.M:
                    AnnotationToolbar.SelectTool(AnnotationTool.Marker);
                    e.Handled = true;
                    return;
                case Key.T:
                    AnnotationToolbar.SelectTool(AnnotationTool.Text);
                    e.Handled = true;
                    return;
            }

            // Ctrl+Z for undo
            if (e.Key == Key.Z && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                _commandHistory.Undo();
                DrawingCanvas.InvalidateVisual();
                e.Handled = true;
                return;
            }

            // Ctrl+Y for redo
            if (e.Key == Key.Y && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                _commandHistory.Redo();
                DrawingCanvas.InvalidateVisual();
                e.Handled = true;
                return;
            }

            // Ctrl+C for copy
            if (e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                ConfirmSelection();
                e.Handled = true;
                return;
            }
        }

        switch (e.Key)
        {
            case Key.Escape:
                Cancel();
                break;

            case Key.Enter:
                if (_hasSelection)
                {
                    ConfirmSelection();
                }
                break;

            // Arrow key movement
            case Key.Left:
            case Key.Right:
            case Key.Up:
            case Key.Down:
                if (_hasSelection && !_isInEditMode)
                {
                    int delta = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 10 : 1;
                    MoveSelection(e.Key, delta);
                    e.Handled = true;
                }
                break;
        }
    }

    private void MoveSelection(Key key, int delta)
    {
        double dx = 0, dy = 0;

        switch (key)
        {
            case Key.Left: dx = -delta; break;
            case Key.Right: dx = delta; break;
            case Key.Up: dy = -delta; break;
            case Key.Down: dy = delta; break;
        }

        double x = _selectionRect.X + dx;
        double y = _selectionRect.Y + dy;

        // Clamp to canvas
        x = Math.Max(0, Math.Min(x, SelectionCanvas.ActualWidth - _selectionRect.Width));
        y = Math.Max(0, Math.Min(y, SelectionCanvas.ActualHeight - _selectionRect.Height));

        _selectionRect = new Rect(x, y, _selectionRect.Width, _selectionRect.Height);
        UpdateDimOverlays(_selectionRect);
        UpdateSelectionBorder();
        UpdateDimensionsDisplay();
        UpdateResizeHandles();
    }

    private void Cancel()
    {
        SelectionCancelled?.Invoke(this, EventArgs.Empty);
        Close();
    }

    private void ConfirmSelection()
    {
        SelectionConfirmed?.Invoke(this, new SelectionConfirmedEventArgs(
            SelectedRegion,
            _screenshotBackground,
            DrawingCanvas));
    }

    #endregion
}

/// <summary>
/// Event args for selection confirmed event.
/// </summary>
public class SelectionConfirmedEventArgs : EventArgs
{
    public DrawingRectangle SelectedRegion { get; }
    public BitmapSource? Screenshot { get; }
    public DrawingCanvas? AnnotationCanvas { get; }

    public SelectionConfirmedEventArgs(DrawingRectangle selectedRegion, BitmapSource? screenshot, DrawingCanvas? annotationCanvas = null)
    {
        SelectedRegion = selectedRegion;
        Screenshot = screenshot;
        AnnotationCanvas = annotationCanvas;
    }
}
