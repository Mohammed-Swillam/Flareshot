using Flareshot.Core.Drawing;
using System.Windows;
using System.Windows.Media;

namespace Flareshot.Tests;

/// <summary>
/// Additional annotation tests for completeness.
/// </summary>
public class AnnotationSerializationTests
{
    [Fact]
    public void Annotation_Color_ShouldDefaultToRed()
    {
        // Arrange & Act
        var annotation = new LineAnnotation();

        // Assert (default is Red, not Black)
        Assert.Equal(Colors.Red, annotation.Color);
    }

    [Fact]
    public void Annotation_StrokeWidth_ShouldDefaultTo3()
    {
        // Arrange & Act
        var annotation = new LineAnnotation();

        // Assert (default is 3, not 2)
        Assert.Equal(3, annotation.StrokeWidth);
    }

    [Fact]
    public void Annotation_IsPreview_ShouldDefaultToFalse()
    {
        // Arrange & Act
        var annotation = new LineAnnotation();

        // Assert
        Assert.False(annotation.IsPreview);
    }

    [Fact]
    public void ArrowAnnotation_ShouldInheritFromAnnotation()
    {
        // Arrange
        var arrow = new ArrowAnnotation
        {
            Color = Colors.Red,
            StrokeWidth = 5,
            StartPoint = new Point(0, 0),
            EndPoint = new Point(100, 100)
        };

        // Assert
        Assert.IsAssignableFrom<Annotation>(arrow);
        Assert.Equal(Colors.Red, arrow.Color);
        Assert.Equal(5, arrow.StrokeWidth);
    }

    [Fact]
    public void RectangleAnnotation_ShouldStoreStartAndEndPoints()
    {
        // Arrange
        var rect = new RectangleAnnotation
        {
            StartPoint = new Point(10, 20),
            EndPoint = new Point(100, 200)
        };

        // Assert
        Assert.Equal(10, rect.StartPoint.X);
        Assert.Equal(20, rect.StartPoint.Y);
        Assert.Equal(100, rect.EndPoint.X);
        Assert.Equal(200, rect.EndPoint.Y);
    }

    [Fact]
    public void PencilAnnotation_Points_ShouldBeInitiallyEmpty()
    {
        // Arrange & Act
        var pencil = new PencilAnnotation();

        // Assert
        Assert.NotNull(pencil.Points);
        Assert.Empty(pencil.Points);
    }

    [Fact]
    public void PencilAnnotation_ShouldAllowAddingPoints()
    {
        // Arrange
        var pencil = new PencilAnnotation();

        // Act
        pencil.Points.Add(new Point(10, 10));
        pencil.Points.Add(new Point(20, 20));
        pencil.Points.Add(new Point(30, 30));

        // Assert
        Assert.Equal(3, pencil.Points.Count);
    }

    [Fact]
    public void MarkerAnnotation_Points_ShouldBeInitiallyEmpty()
    {
        // Arrange & Act
        var marker = new MarkerAnnotation();

        // Assert
        Assert.NotNull(marker.Points);
        Assert.Empty(marker.Points);
    }

    [Fact]
    public void TextAnnotation_ShouldHaveDefaultFontSize()
    {
        // Arrange & Act
        var text = new TextAnnotation { Text = "Test" };

        // Assert
        Assert.Equal(16, text.FontSize);
    }
}

/// <summary>
/// Tests for add/remove annotation commands.
/// </summary>
public class AnnotationCommandTests
{
    private class MockAnnotationCollection : IAnnotationCollection
    {
        private readonly List<Annotation> _annotations = new();
        public int AddCount { get; private set; }
        public int RemoveCount { get; private set; }

        public void Add(Annotation annotation)
        {
            _annotations.Add(annotation);
            AddCount++;
        }

        public void Remove(Annotation annotation)
        {
            _annotations.Remove(annotation);
            RemoveCount++;
        }

        public IReadOnlyList<Annotation> GetAll() => _annotations.AsReadOnly();
        public void Clear() => _annotations.Clear();
    }

    [Fact]
    public void AddAnnotationCommand_Execute_ShouldAddToCollection()
    {
        // Arrange
        var collection = new MockAnnotationCollection();
        var annotation = new LineAnnotation();
        var command = new AddAnnotationCommand(collection, annotation);

        // Act
        command.Execute();

        // Assert
        Assert.Equal(1, collection.AddCount);
        Assert.Contains(annotation, collection.GetAll());
    }

    [Fact]
    public void AddAnnotationCommand_Undo_ShouldRemoveFromCollection()
    {
        // Arrange
        var collection = new MockAnnotationCollection();
        var annotation = new LineAnnotation();
        var command = new AddAnnotationCommand(collection, annotation);
        command.Execute();

        // Act
        command.Undo();

        // Assert
        Assert.Equal(1, collection.RemoveCount);
        Assert.DoesNotContain(annotation, collection.GetAll());
    }

    [Fact]
    public void RemoveAnnotationCommand_Execute_ShouldRemoveFromCollection()
    {
        // Arrange
        var collection = new MockAnnotationCollection();
        var annotation = new LineAnnotation();
        collection.Add(annotation);
        var command = new RemoveAnnotationCommand(collection, annotation);

        // Act
        command.Execute();

        // Assert
        Assert.Equal(1, collection.RemoveCount); // Just the Execute call
        Assert.DoesNotContain(annotation, collection.GetAll());
    }

    [Fact]
    public void RemoveAnnotationCommand_Undo_ShouldAddBackToCollection()
    {
        // Arrange
        var collection = new MockAnnotationCollection();
        var annotation = new LineAnnotation();
        collection.Add(annotation);
        var command = new RemoveAnnotationCommand(collection, annotation);
        command.Execute();

        // Act
        command.Undo();

        // Assert
        Assert.Contains(annotation, collection.GetAll());
    }
}

/// <summary>
/// Tests for annotation bounds calculations.
/// </summary>
public class AnnotationBoundsTests
{
    [Fact]
    public void LineAnnotation_GetBounds_WithReversedPoints_ShouldNormalize()
    {
        // Arrange - end point is before start point
        var line = new LineAnnotation
        {
            StartPoint = new Point(100, 100),
            EndPoint = new Point(10, 10)
        };

        // Act
        var bounds = line.GetBounds();

        // Assert - bounds should be normalized
        Assert.Equal(10, bounds.Left);
        Assert.Equal(10, bounds.Top);
        Assert.Equal(100, bounds.Right);
        Assert.Equal(100, bounds.Bottom);
    }

    [Fact]
    public void ArrowAnnotation_GetBounds_ShouldIncludeArrowHeadInflation()
    {
        // Arrange
        var arrow = new ArrowAnnotation
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(50, 75)
        };

        // Act
        var bounds = arrow.GetBounds();

        // Assert - bounds are inflated by ArrowHeadLength (15) to include arrowhead
        Assert.True(bounds.Left < 0); // Inflated
        Assert.True(bounds.Top < 0);  // Inflated
        Assert.True(bounds.Right > 50); // Inflated
        Assert.True(bounds.Bottom > 75); // Inflated
    }

    [Fact]
    public void RectangleAnnotation_GetBounds_WithReversedPoints_ShouldNormalize()
    {
        // Arrange
        var rect = new RectangleAnnotation
        {
            StartPoint = new Point(200, 150),
            EndPoint = new Point(50, 25)
        };

        // Act
        var bounds = rect.GetBounds();

        // Assert
        Assert.Equal(50, bounds.Left);
        Assert.Equal(25, bounds.Top);
        Assert.Equal(200, bounds.Right);
        Assert.Equal(150, bounds.Bottom);
    }

    [Fact]
    public void PencilAnnotation_GetBounds_WithSinglePoint_ShouldReturnPointBounds()
    {
        // Arrange
        var pencil = new PencilAnnotation();
        pencil.Points.Add(new Point(50, 50));

        // Act
        var bounds = pencil.GetBounds();

        // Assert
        Assert.Equal(50, bounds.Left);
        Assert.Equal(50, bounds.Top);
        Assert.Equal(50, bounds.Right);
        Assert.Equal(50, bounds.Bottom);
    }

    [Fact]
    public void PencilAnnotation_GetBounds_Empty_ShouldReturnEmptyRect()
    {
        // Arrange
        var pencil = new PencilAnnotation();

        // Act
        var bounds = pencil.GetBounds();

        // Assert
        Assert.Equal(Rect.Empty, bounds);
    }
}
