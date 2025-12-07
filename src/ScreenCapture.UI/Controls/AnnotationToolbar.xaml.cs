using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ScreenCapture.Core.Drawing;

using UserControl = System.Windows.Controls.UserControl;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Button = System.Windows.Controls.Button;

namespace ScreenCapture.UI.Controls;

/// <summary>
/// Toolbar for annotation tools.
/// </summary>
public partial class AnnotationToolbar : UserControl
{
    private AnnotationTool _selectedTool = AnnotationTool.None;
    private Color _selectedColor = Colors.Red;
    private readonly List<ToggleButton> _toolButtons = new();

    /// <summary>
    /// Event raised when tool selection changes.
    /// </summary>
    public event EventHandler<AnnotationTool>? ToolChanged;

    /// <summary>
    /// Event raised when color selection changes.
    /// </summary>
    public event EventHandler<Color>? ColorChanged;

    /// <summary>
    /// Event raised when undo is requested.
    /// </summary>
    public event EventHandler? UndoRequested;

    /// <summary>
    /// Event raised when redo is requested.
    /// </summary>
    public event EventHandler? RedoRequested;

    /// <summary>
    /// Gets the currently selected tool.
    /// </summary>
    public AnnotationTool SelectedTool => _selectedTool;

    /// <summary>
    /// Gets the currently selected color.
    /// </summary>
    public Color SelectedColor => _selectedColor;

    public AnnotationToolbar()
    {
        InitializeComponent();

        // Collect all tool buttons for mutual exclusion
        _toolButtons.Add(PencilButton);
        _toolButtons.Add(LineButton);
        _toolButtons.Add(ArrowButton);
        _toolButtons.Add(RectangleButton);
        _toolButtons.Add(MarkerButton);
        _toolButtons.Add(TextButton);
    }

    private void SelectTool(AnnotationTool tool, ToggleButton button)
    {
        // Uncheck other buttons
        foreach (var btn in _toolButtons)
        {
            if (btn != button)
            {
                btn.IsChecked = false;
            }
        }

        // Toggle the selected button
        if (button.IsChecked == true)
        {
            _selectedTool = tool;
        }
        else
        {
            _selectedTool = AnnotationTool.None;
        }

        ToolChanged?.Invoke(this, _selectedTool);
    }

    private void PencilButton_Click(object sender, RoutedEventArgs e)
    {
        SelectTool(AnnotationTool.Pencil, PencilButton);
    }

    private void LineButton_Click(object sender, RoutedEventArgs e)
    {
        SelectTool(AnnotationTool.Line, LineButton);
    }

    private void ArrowButton_Click(object sender, RoutedEventArgs e)
    {
        SelectTool(AnnotationTool.Arrow, ArrowButton);
    }

    private void RectangleButton_Click(object sender, RoutedEventArgs e)
    {
        SelectTool(AnnotationTool.Rectangle, RectangleButton);
    }

    private void MarkerButton_Click(object sender, RoutedEventArgs e)
    {
        SelectTool(AnnotationTool.Marker, MarkerButton);
    }

    private void TextButton_Click(object sender, RoutedEventArgs e)
    {
        SelectTool(AnnotationTool.Text, TextButton);
    }

    private void ColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string colorHex)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(colorHex);
                _selectedColor = color;
                CurrentColorButton.Background = new SolidColorBrush(color);
                ColorChanged?.Invoke(this, _selectedColor);
            }
            catch
            {
                // Ignore invalid color
            }
        }
    }

    private void CurrentColorButton_Click(object sender, RoutedEventArgs e)
    {
        // Show Windows color dialog
        using var colorDialog = new System.Windows.Forms.ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(_selectedColor.A, _selectedColor.R, _selectedColor.G, _selectedColor.B),
            FullOpen = true
        };

        if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var result = colorDialog.Color;
            _selectedColor = Color.FromArgb(result.A, result.R, result.G, result.B);
            CurrentColorButton.Background = new SolidColorBrush(_selectedColor);
            ColorChanged?.Invoke(this, _selectedColor);
        }
    }

    private void UndoButton_Click(object sender, RoutedEventArgs e)
    {
        UndoRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RedoButton_Click(object sender, RoutedEventArgs e)
    {
        RedoRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Updates the enabled state of undo/redo buttons.
    /// </summary>
    public void UpdateUndoRedoState(bool canUndo, bool canRedo)
    {
        UndoButton.IsEnabled = canUndo;
        RedoButton.IsEnabled = canRedo;
    }

    /// <summary>
    /// Selects a tool programmatically.
    /// </summary>
    public void SelectTool(AnnotationTool tool)
    {
        foreach (var btn in _toolButtons)
        {
            btn.IsChecked = false;
        }

        _selectedTool = tool;

        switch (tool)
        {
            case AnnotationTool.Pencil:
                PencilButton.IsChecked = true;
                break;
            case AnnotationTool.Line:
                LineButton.IsChecked = true;
                break;
            case AnnotationTool.Arrow:
                ArrowButton.IsChecked = true;
                break;
            case AnnotationTool.Rectangle:
                RectangleButton.IsChecked = true;
                break;
            case AnnotationTool.Marker:
                MarkerButton.IsChecked = true;
                break;
            case AnnotationTool.Text:
                TextButton.IsChecked = true;
                break;
        }

        ToolChanged?.Invoke(this, _selectedTool);
    }

    /// <summary>
    /// Sets the current color programmatically.
    /// </summary>
    public void SetColor(Color color)
    {
        _selectedColor = color;
        CurrentColorButton.Background = new SolidColorBrush(color);
        ColorChanged?.Invoke(this, _selectedColor);
    }
}
