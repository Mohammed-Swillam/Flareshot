namespace Flareshot.Core.Drawing;

/// <summary>
/// Interface for drawing commands (for undo/redo).
/// </summary>
public interface IDrawingCommand
{
    /// <summary>
    /// Executes the command.
    /// </summary>
    void Execute();

    /// <summary>
    /// Undoes the command.
    /// </summary>
    void Undo();
}

/// <summary>
/// Command for adding an annotation.
/// </summary>
public class AddAnnotationCommand : IDrawingCommand
{
    private readonly IAnnotationCollection _collection;
    private readonly Annotation _annotation;

    public AddAnnotationCommand(IAnnotationCollection collection, Annotation annotation)
    {
        _collection = collection;
        _annotation = annotation;
    }

    public void Execute()
    {
        _collection.Add(_annotation);
    }

    public void Undo()
    {
        _collection.Remove(_annotation);
    }
}

/// <summary>
/// Command for removing an annotation.
/// </summary>
public class RemoveAnnotationCommand : IDrawingCommand
{
    private readonly IAnnotationCollection _collection;
    private readonly Annotation _annotation;

    public RemoveAnnotationCommand(IAnnotationCollection collection, Annotation annotation)
    {
        _collection = collection;
        _annotation = annotation;
    }

    public void Execute()
    {
        _collection.Remove(_annotation);
    }

    public void Undo()
    {
        _collection.Add(_annotation);
    }
}

/// <summary>
/// Interface for annotation collection.
/// </summary>
public interface IAnnotationCollection
{
    void Add(Annotation annotation);
    void Remove(Annotation annotation);
    IReadOnlyList<Annotation> GetAll();
    void Clear();
}

/// <summary>
/// Manages command history for undo/redo operations.
/// </summary>
public class CommandHistory
{
    private readonly Stack<IDrawingCommand> _undoStack = new();
    private readonly Stack<IDrawingCommand> _redoStack = new();

    /// <summary>
    /// Gets whether there are commands to undo.
    /// </summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>
    /// Gets whether there are commands to redo.
    /// </summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// Gets the number of commands in the undo stack.
    /// </summary>
    public int UndoCount => _undoStack.Count;

    /// <summary>
    /// Gets the number of commands in the redo stack.
    /// </summary>
    public int RedoCount => _redoStack.Count;

    /// <summary>
    /// Event raised when the history state changes.
    /// </summary>
    public event EventHandler? HistoryChanged;

    /// <summary>
    /// Executes a command and adds it to the history.
    /// </summary>
    public void Execute(IDrawingCommand command)
    {
        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear(); // Clear redo stack when new command is executed
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Undoes the last command.
    /// </summary>
    public void Undo()
    {
        if (!CanUndo) return;

        var command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Redoes the last undone command.
    /// </summary>
    public void Redo()
    {
        if (!CanRedo) return;

        var command = _redoStack.Pop();
        command.Execute();
        _undoStack.Push(command);
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Clears all history.
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }
}
