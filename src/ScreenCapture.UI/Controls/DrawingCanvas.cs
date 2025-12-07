using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenCapture.Core.Drawing;

namespace ScreenCapture.UI.Controls;

/// <summary>
/// Custom canvas for rendering annotations using DrawingVisual.
/// </summary>
public class DrawingCanvas : FrameworkElement, IAnnotationCollection
{
    private readonly List<Annotation> _annotations = new();
    private readonly VisualCollection _visuals;
    private Annotation? _previewAnnotation;

    /// <summary>
    /// Event raised when annotations change.
    /// </summary>
    public event EventHandler? AnnotationsChanged;

    public DrawingCanvas()
    {
        _visuals = new VisualCollection(this);
    }

    /// <summary>
    /// Adds an annotation to the canvas.
    /// </summary>
    public void Add(Annotation annotation)
    {
        _annotations.Add(annotation);
        InvalidateVisual();
        AnnotationsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Removes an annotation from the canvas.
    /// </summary>
    public void Remove(Annotation annotation)
    {
        _annotations.Remove(annotation);
        InvalidateVisual();
        AnnotationsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets all annotations.
    /// </summary>
    public IReadOnlyList<Annotation> GetAll() => _annotations.AsReadOnly();

    /// <summary>
    /// Clears all annotations.
    /// </summary>
    public void Clear()
    {
        _annotations.Clear();
        _previewAnnotation = null;
        InvalidateVisual();
        AnnotationsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sets the preview annotation (shown while drawing).
    /// </summary>
    public void SetPreview(Annotation? annotation)
    {
        _previewAnnotation = annotation;
        InvalidateVisual();
    }

    /// <summary>
    /// Gets the number of annotations.
    /// </summary>
    public int Count => _annotations.Count;

    /// <summary>
    /// Gets whether there are any annotations.
    /// </summary>
    public bool HasAnnotations => _annotations.Count > 0 || _previewAnnotation != null;

    protected override int VisualChildrenCount => _visuals.Count;

    protected override Visual GetVisualChild(int index) => _visuals[index];

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        // Render all completed annotations
        foreach (var annotation in _annotations)
        {
            annotation.Render(drawingContext);
        }

        // Render preview annotation if any
        _previewAnnotation?.Render(drawingContext);
    }

    /// <summary>
    /// Renders all annotations to a bitmap.
    /// </summary>
    public RenderTargetBitmap RenderToBitmap(int width, int height, double dpi = 96)
    {
        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            foreach (var annotation in _annotations)
            {
                annotation.Render(context);
            }
        }

        var bitmap = new RenderTargetBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();

        return bitmap;
    }
}
