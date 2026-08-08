namespace Novolis.Avalonia.Controls.Sketch;

/// <summary>Snapshot-based undo/redo stack for sketch elements.</summary>
public sealed class SketchHistory
{
    readonly List<IReadOnlyList<StrokeShape>> _undo = [];
    readonly List<IReadOnlyList<StrokeShape>> _redo = [];
    readonly int _capacity;

    /// <summary>Creates a history with the given capacity (default 100).</summary>
    public SketchHistory(int capacity = 100) =>
        _capacity = Math.Max(1, capacity);

    /// <summary>Whether <see cref="PopUndo"/> can restore a prior snapshot.</summary>
    public bool CanUndo => _undo.Count > 0;

    /// <summary>Whether <see cref="PopRedo"/> can restore a forward snapshot.</summary>
    public bool CanRedo => _redo.Count > 0;

    /// <summary>Pushes a deep clone of <paramref name="elements"/> before a mutation.</summary>
    public void PushBeforeChange(IReadOnlyList<StrokeShape> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        _undo.Add(CloneAll(elements));
        if (_undo.Count > _capacity)
            _undo.RemoveAt(0);
        _redo.Clear();
    }

    /// <summary>
    /// Pops the last undo snapshot. Caller should push the current state onto redo
    /// via the returned pair: (previous snapshot to restore, current to keep for redo).
    /// </summary>
    public IReadOnlyList<StrokeShape>? PopUndo(IReadOnlyList<StrokeShape> current)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (_undo.Count == 0)
            return null;
        var prior = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        _redo.Add(CloneAll(current));
        return prior;
    }

    /// <summary>Pops the last redo snapshot, pushing <paramref name="current"/> onto undo.</summary>
    public IReadOnlyList<StrokeShape>? PopRedo(IReadOnlyList<StrokeShape> current)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (_redo.Count == 0)
            return null;
        var next = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        _undo.Add(CloneAll(current));
        return next;
    }

    /// <summary>Clears undo and redo stacks.</summary>
    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }

    static IReadOnlyList<StrokeShape> CloneAll(IReadOnlyList<StrokeShape> elements)
    {
        var list = new List<StrokeShape>(elements.Count);
        foreach (var e in elements)
            list.Add(e.Clone());
        return list;
    }
}
