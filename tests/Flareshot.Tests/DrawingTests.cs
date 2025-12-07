using Flareshot.Core.Drawing;
using System.Windows;

namespace Flareshot.Tests;

/// <summary>
/// Unit tests for CommandHistory (undo/redo system).
/// </summary>
public class CommandHistoryTests
{
    /// <summary>
    /// Mock annotation collection for testing commands.
    /// </summary>
    private class MockAnnotationCollection : IAnnotationCollection
    {
        private readonly List<Annotation> _annotations = new();

        public int Count => _annotations.Count;

        public void Add(Annotation annotation) => _annotations.Add(annotation);
        public void Remove(Annotation annotation) => _annotations.Remove(annotation);
        public IReadOnlyList<Annotation> GetAll() => _annotations.AsReadOnly();
        public void Clear() => _annotations.Clear();
    }

    [Fact]
    public void Execute_ShouldAddToUndoStack()
    {
        // Arrange
        var history = new CommandHistory();
        var collection = new MockAnnotationCollection();
        var annotation = new LineAnnotation { StartPoint = new Point(0, 0), EndPoint = new Point(10, 10) };
        var command = new AddAnnotationCommand(collection, annotation);

        // Act
        history.Execute(command);

        // Assert
        Assert.True(history.CanUndo);
        Assert.Equal(1, history.UndoCount);
        Assert.Equal(1, collection.Count);
    }

    [Fact]
    public void Undo_ShouldMoveToRedoStack()
    {
        // Arrange
        var history = new CommandHistory();
        var collection = new MockAnnotationCollection();
        var annotation = new LineAnnotation { StartPoint = new Point(0, 0), EndPoint = new Point(10, 10) };
        var command = new AddAnnotationCommand(collection, annotation);
        history.Execute(command);

        // Act
        history.Undo();

        // Assert
        Assert.False(history.CanUndo);
        Assert.True(history.CanRedo);
        Assert.Equal(0, history.UndoCount);
        Assert.Equal(1, history.RedoCount);
        Assert.Equal(0, collection.Count);
    }

    [Fact]
    public void Redo_ShouldMoveBackToUndoStack()
    {
        // Arrange
        var history = new CommandHistory();
        var collection = new MockAnnotationCollection();
        var annotation = new LineAnnotation { StartPoint = new Point(0, 0), EndPoint = new Point(10, 10) };
        var command = new AddAnnotationCommand(collection, annotation);
        history.Execute(command);
        history.Undo();

        // Act
        history.Redo();

        // Assert
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.Equal(1, history.UndoCount);
        Assert.Equal(0, history.RedoCount);
        Assert.Equal(1, collection.Count);
    }

    [Fact]
    public void Execute_AfterUndo_ShouldClearRedoStack()
    {
        // Arrange
        var history = new CommandHistory();
        var collection = new MockAnnotationCollection();
        var annotation1 = new LineAnnotation { StartPoint = new Point(0, 0), EndPoint = new Point(10, 10) };
        var annotation2 = new LineAnnotation { StartPoint = new Point(20, 20), EndPoint = new Point(30, 30) };
        
        history.Execute(new AddAnnotationCommand(collection, annotation1));
        history.Undo();

        // Act
        history.Execute(new AddAnnotationCommand(collection, annotation2));

        // Assert
        Assert.False(history.CanRedo); // Redo stack should be cleared
        Assert.Equal(1, history.UndoCount);
    }

    [Fact]
    public void HistoryChanged_ShouldFireOnExecute()
    {
        // Arrange
        var history = new CommandHistory();
        var collection = new MockAnnotationCollection();
        var annotation = new LineAnnotation { StartPoint = new Point(0, 0), EndPoint = new Point(10, 10) };
        var eventFired = false;
        history.HistoryChanged += (s, e) => eventFired = true;

        // Act
        history.Execute(new AddAnnotationCommand(collection, annotation));

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void HistoryChanged_ShouldFireOnUndo()
    {
        // Arrange
        var history = new CommandHistory();
        var collection = new MockAnnotationCollection();
        var annotation = new LineAnnotation { StartPoint = new Point(0, 0), EndPoint = new Point(10, 10) };
        history.Execute(new AddAnnotationCommand(collection, annotation));
        
        var eventFired = false;
        history.HistoryChanged += (s, e) => eventFired = true;

        // Act
        history.Undo();

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void Clear_ShouldResetAllStacks()
    {
        // Arrange
        var history = new CommandHistory();
        var collection = new MockAnnotationCollection();
        var annotation = new LineAnnotation { StartPoint = new Point(0, 0), EndPoint = new Point(10, 10) };
        history.Execute(new AddAnnotationCommand(collection, annotation));
        history.Undo();

        // Act
        history.Clear();

        // Assert
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.Equal(0, history.UndoCount);
        Assert.Equal(0, history.RedoCount);
    }

    [Fact]
    public void Undo_WhenEmpty_ShouldDoNothing()
    {
        // Arrange
        var history = new CommandHistory();

        // Act & Assert (should not throw)
        history.Undo();
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Redo_WhenEmpty_ShouldDoNothing()
    {
        // Arrange
        var history = new CommandHistory();

        // Act & Assert (should not throw)
        history.Redo();
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }
}

/// <summary>
/// Unit tests for Annotation models.
/// </summary>
public class AnnotationTests
{
    [Fact]
    public void LineAnnotation_GetBounds_ShouldReturnCorrectRect()
    {
        // Arrange
        var annotation = new LineAnnotation
        {
            StartPoint = new Point(10, 20),
            EndPoint = new Point(50, 80)
        };

        // Act
        var bounds = annotation.GetBounds();

        // Assert
        Assert.Equal(10, bounds.Left);
        Assert.Equal(20, bounds.Top);
        Assert.Equal(50, bounds.Right);
        Assert.Equal(80, bounds.Bottom);
    }

    [Fact]
    public void RectangleAnnotation_GetBounds_ShouldReturnCorrectRect()
    {
        // Arrange
        var annotation = new RectangleAnnotation
        {
            StartPoint = new Point(100, 100),
            EndPoint = new Point(200, 150)
        };

        // Act
        var bounds = annotation.GetBounds();

        // Assert
        Assert.Equal(100, bounds.Left);
        Assert.Equal(100, bounds.Top);
        Assert.Equal(200, bounds.Right);
        Assert.Equal(150, bounds.Bottom);
    }

    [Fact]
    public void PencilAnnotation_GetBounds_ShouldEncloseAllPoints()
    {
        // Arrange
        var annotation = new PencilAnnotation();
        annotation.Points.Add(new Point(10, 10));
        annotation.Points.Add(new Point(50, 30));
        annotation.Points.Add(new Point(25, 60));

        // Act
        var bounds = annotation.GetBounds();

        // Assert
        Assert.Equal(10, bounds.Left);
        Assert.Equal(10, bounds.Top);
        Assert.Equal(50, bounds.Right);
        Assert.Equal(60, bounds.Bottom);
    }

    [Fact]
    public void TextAnnotation_ShouldStoreTextAndPosition()
    {
        // Arrange & Act
        var annotation = new TextAnnotation
        {
            Text = "Hello World",
            Position = new Point(100, 200)
        };

        // Assert
        Assert.Equal("Hello World", annotation.Text);
        Assert.Equal(100, annotation.Position.X);
        Assert.Equal(200, annotation.Position.Y);
    }

    [Fact]
    public void MarkerAnnotation_ShouldHaveWiderStroke()
    {
        // Arrange & Act
        var marker = new MarkerAnnotation();
        var pencil = new PencilAnnotation();

        // Assert
        Assert.True(marker.StrokeWidth > pencil.StrokeWidth);
        Assert.Equal(20, marker.StrokeWidth); // Default marker width
    }
}
