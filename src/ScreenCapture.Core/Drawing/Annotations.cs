namespace ScreenCapture.Core.Drawing;

/// <summary>
/// Available annotation tools.
/// </summary>
public enum AnnotationTool
{
    None,
    Pencil,
    Line,
    Arrow,
    Rectangle,
    Marker,
    Text
}

/// <summary>
/// Base class for all annotations.
/// </summary>
public abstract class Annotation
{
    /// <summary>
    /// Unique identifier for this annotation.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// The color of the annotation.
    /// </summary>
    public System.Windows.Media.Color Color { get; set; } = System.Windows.Media.Colors.Red;

    /// <summary>
    /// The stroke width for drawing.
    /// </summary>
    public double StrokeWidth { get; set; } = 3;

    /// <summary>
    /// When the annotation was created.
    /// </summary>
    public DateTime CreatedAt { get; } = DateTime.Now;

    /// <summary>
    /// Whether this annotation is currently being drawn (preview mode).
    /// </summary>
    public bool IsPreview { get; set; }

    /// <summary>
    /// Renders the annotation using the provided DrawingContext.
    /// </summary>
    public abstract void Render(System.Windows.Media.DrawingContext dc);

    /// <summary>
    /// Gets the bounding rectangle of the annotation.
    /// </summary>
    public abstract System.Windows.Rect GetBounds();
}

/// <summary>
/// Pencil/freehand drawing annotation.
/// </summary>
public class PencilAnnotation : Annotation
{
    public List<System.Windows.Point> Points { get; } = new();

    public override void Render(System.Windows.Media.DrawingContext dc)
    {
        if (Points.Count < 2) return;

        var pen = new System.Windows.Media.Pen(
            new System.Windows.Media.SolidColorBrush(Color),
            StrokeWidth)
        {
            StartLineCap = System.Windows.Media.PenLineCap.Round,
            EndLineCap = System.Windows.Media.PenLineCap.Round,
            LineJoin = System.Windows.Media.PenLineJoin.Round
        };
        pen.Freeze();

        var geometry = new System.Windows.Media.StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(Points[0], false, false);
            for (int i = 1; i < Points.Count; i++)
            {
                ctx.LineTo(Points[i], true, true);
            }
        }
        geometry.Freeze();

        dc.DrawGeometry(null, pen, geometry);
    }

    public override System.Windows.Rect GetBounds()
    {
        if (Points.Count == 0) return System.Windows.Rect.Empty;

        double minX = Points.Min(p => p.X);
        double minY = Points.Min(p => p.Y);
        double maxX = Points.Max(p => p.X);
        double maxY = Points.Max(p => p.Y);

        return new System.Windows.Rect(minX, minY, maxX - minX, maxY - minY);
    }
}

/// <summary>
/// Straight line annotation.
/// </summary>
public class LineAnnotation : Annotation
{
    public System.Windows.Point StartPoint { get; set; }
    public System.Windows.Point EndPoint { get; set; }

    public override void Render(System.Windows.Media.DrawingContext dc)
    {
        var pen = new System.Windows.Media.Pen(
            new System.Windows.Media.SolidColorBrush(Color),
            StrokeWidth)
        {
            StartLineCap = System.Windows.Media.PenLineCap.Round,
            EndLineCap = System.Windows.Media.PenLineCap.Round
        };
        pen.Freeze();

        dc.DrawLine(pen, StartPoint, EndPoint);
    }

    public override System.Windows.Rect GetBounds()
    {
        return new System.Windows.Rect(StartPoint, EndPoint);
    }
}

/// <summary>
/// Arrow annotation with arrowhead.
/// </summary>
public class ArrowAnnotation : Annotation
{
    public System.Windows.Point StartPoint { get; set; }
    public System.Windows.Point EndPoint { get; set; }

    private const double ArrowHeadLength = 15;
    private const double ArrowHeadAngle = 25; // degrees

    public override void Render(System.Windows.Media.DrawingContext dc)
    {
        var brush = new System.Windows.Media.SolidColorBrush(Color);
        brush.Freeze();

        var pen = new System.Windows.Media.Pen(brush, StrokeWidth)
        {
            StartLineCap = System.Windows.Media.PenLineCap.Round,
            EndLineCap = System.Windows.Media.PenLineCap.Round
        };
        pen.Freeze();

        // Draw the line
        dc.DrawLine(pen, StartPoint, EndPoint);

        // Calculate arrowhead
        double angle = Math.Atan2(EndPoint.Y - StartPoint.Y, EndPoint.X - StartPoint.X);
        double arrowAngleRad = ArrowHeadAngle * Math.PI / 180;

        var arrowPoint1 = new System.Windows.Point(
            EndPoint.X - ArrowHeadLength * Math.Cos(angle - arrowAngleRad),
            EndPoint.Y - ArrowHeadLength * Math.Sin(angle - arrowAngleRad));

        var arrowPoint2 = new System.Windows.Point(
            EndPoint.X - ArrowHeadLength * Math.Cos(angle + arrowAngleRad),
            EndPoint.Y - ArrowHeadLength * Math.Sin(angle + arrowAngleRad));

        // Draw filled arrowhead
        var arrowGeometry = new System.Windows.Media.StreamGeometry();
        using (var ctx = arrowGeometry.Open())
        {
            ctx.BeginFigure(EndPoint, true, true);
            ctx.LineTo(arrowPoint1, true, false);
            ctx.LineTo(arrowPoint2, true, false);
        }
        arrowGeometry.Freeze();

        dc.DrawGeometry(brush, null, arrowGeometry);
    }

    public override System.Windows.Rect GetBounds()
    {
        var rect = new System.Windows.Rect(StartPoint, EndPoint);
        rect.Inflate(ArrowHeadLength, ArrowHeadLength);
        return rect;
    }
}

/// <summary>
/// Rectangle annotation (hollow).
/// </summary>
public class RectangleAnnotation : Annotation
{
    public System.Windows.Point StartPoint { get; set; }
    public System.Windows.Point EndPoint { get; set; }

    public override void Render(System.Windows.Media.DrawingContext dc)
    {
        var pen = new System.Windows.Media.Pen(
            new System.Windows.Media.SolidColorBrush(Color),
            StrokeWidth);
        pen.Freeze();

        var rect = new System.Windows.Rect(StartPoint, EndPoint);
        dc.DrawRectangle(null, pen, rect);
    }

    public override System.Windows.Rect GetBounds()
    {
        return new System.Windows.Rect(StartPoint, EndPoint);
    }
}

/// <summary>
/// Marker/highlighter annotation (semi-transparent wide stroke).
/// </summary>
public class MarkerAnnotation : Annotation
{
    public List<System.Windows.Point> Points { get; } = new();

    public MarkerAnnotation()
    {
        StrokeWidth = 20; // Wider stroke for highlighter
    }

    public override void Render(System.Windows.Media.DrawingContext dc)
    {
        if (Points.Count < 2) return;

        // Semi-transparent color for highlighter effect
        var highlightColor = System.Windows.Media.Color.FromArgb(
            128, // 50% opacity
            Color.R,
            Color.G,
            Color.B);

        var pen = new System.Windows.Media.Pen(
            new System.Windows.Media.SolidColorBrush(highlightColor),
            StrokeWidth)
        {
            StartLineCap = System.Windows.Media.PenLineCap.Flat,
            EndLineCap = System.Windows.Media.PenLineCap.Flat,
            LineJoin = System.Windows.Media.PenLineJoin.Round
        };
        pen.Freeze();

        var geometry = new System.Windows.Media.StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(Points[0], false, false);
            for (int i = 1; i < Points.Count; i++)
            {
                ctx.LineTo(Points[i], true, true);
            }
        }
        geometry.Freeze();

        dc.DrawGeometry(null, pen, geometry);
    }

    public override System.Windows.Rect GetBounds()
    {
        if (Points.Count == 0) return System.Windows.Rect.Empty;

        double minX = Points.Min(p => p.X);
        double minY = Points.Min(p => p.Y);
        double maxX = Points.Max(p => p.X);
        double maxY = Points.Max(p => p.Y);

        var rect = new System.Windows.Rect(minX, minY, maxX - minX, maxY - minY);
        rect.Inflate(StrokeWidth / 2, StrokeWidth / 2);
        return rect;
    }
}

/// <summary>
/// Text annotation.
/// </summary>
public class TextAnnotation : Annotation
{
    public System.Windows.Point Position { get; set; }
    public string Text { get; set; } = string.Empty;
    public double FontSize { get; set; } = 16;
    public string FontFamily { get; set; } = "Segoe UI";

    public override void Render(System.Windows.Media.DrawingContext dc)
    {
        if (string.IsNullOrEmpty(Text)) return;

        var brush = new System.Windows.Media.SolidColorBrush(Color);
        brush.Freeze();

        var typeface = new System.Windows.Media.Typeface(
            new System.Windows.Media.FontFamily(FontFamily),
            System.Windows.FontStyles.Normal,
            System.Windows.FontWeights.Normal,
            System.Windows.FontStretches.Normal);

        var formattedText = new System.Windows.Media.FormattedText(
            Text,
            System.Globalization.CultureInfo.CurrentCulture,
            System.Windows.FlowDirection.LeftToRight,
            typeface,
            FontSize,
            brush,
            96); // DPI

        dc.DrawText(formattedText, Position);
    }

    public override System.Windows.Rect GetBounds()
    {
        if (string.IsNullOrEmpty(Text)) return System.Windows.Rect.Empty;

        var typeface = new System.Windows.Media.Typeface(
            new System.Windows.Media.FontFamily(FontFamily),
            System.Windows.FontStyles.Normal,
            System.Windows.FontWeights.Normal,
            System.Windows.FontStretches.Normal);

        var formattedText = new System.Windows.Media.FormattedText(
            Text,
            System.Globalization.CultureInfo.CurrentCulture,
            System.Windows.FlowDirection.LeftToRight,
            typeface,
            FontSize,
            System.Windows.Media.Brushes.Black,
            96);

        return new System.Windows.Rect(Position, new System.Windows.Size(formattedText.Width, formattedText.Height));
    }
}
