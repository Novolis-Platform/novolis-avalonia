namespace Novolis.Avalonia.Controls;

/// <summary>In-memory sketch document: strokes, selection, grid, and undo history.</summary>
public sealed class SketchDocument
{
    readonly List<StrokeShape> _elements = [];
    readonly HashSet<string> _selection = new(StringComparer.Ordinal);
    readonly SketchHistory _history = new();

    /// <summary>Document format version.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Grid configuration.</summary>
    public GridSettings Grid { get; } = new();

    /// <summary>Committed stroke shapes (z-order = list order).</summary>
    public IReadOnlyList<StrokeShape> Elements => _elements;

    /// <summary>Selected element ids.</summary>
    public IReadOnlyCollection<string> Selection => _selection;

    /// <summary>Raised after any document mutation that should refresh the view.</summary>
    public event Action? Changed;

    /// <summary>Whether undo is available.</summary>
    public bool CanUndo => _history.CanUndo;

    /// <summary>Whether redo is available.</summary>
    public bool CanRedo => _history.CanRedo;

    /// <summary>Finds a stroke by id, or null.</summary>
    public StrokeShape? Find(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        foreach (var e in _elements)
        {
            if (string.Equals(e.Id, id, StringComparison.Ordinal))
                return e;
        }

        return null;
    }

    /// <summary>Adds a committed stroke and records history.</summary>
    public void AddStroke(StrokeShape stroke)
    {
        ArgumentNullException.ThrowIfNull(stroke);
        _history.PushBeforeChange(_elements);
        _elements.Add(stroke);
        Notify();
    }

    /// <summary>Replaces all elements (used by load / undo restore).</summary>
    public void ReplaceElements(IEnumerable<StrokeShape> elements, bool recordHistory = false)
    {
        ArgumentNullException.ThrowIfNull(elements);
        if (recordHistory)
            _history.PushBeforeChange(_elements);
        _elements.Clear();
        foreach (var e in elements)
            _elements.Add(e);
        PruneSelection();
        Notify();
    }

    /// <summary>Removes selected elements.</summary>
    public void DeleteSelection()
    {
        if (_selection.Count == 0)
            return;
        _history.PushBeforeChange(_elements);
        _elements.RemoveAll(e => _selection.Contains(e.Id));
        _selection.Clear();
        Notify();
    }

    /// <summary>Removes strokes with the given ids (records history once).</summary>
    public void DeleteByIds(IEnumerable<string> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var set = new HashSet<string>(ids, StringComparer.Ordinal);
        if (set.Count == 0 || !_elements.Any(e => set.Contains(e.Id)))
            return;
        _history.PushBeforeChange(_elements);
        _elements.RemoveAll(e => set.Contains(e.Id));
        _selection.RemoveWhere(set.Contains);
        Notify();
    }

    /// <summary>Clears all elements and selection.</summary>
    public void Clear()
    {
        if (_elements.Count == 0 && _selection.Count == 0)
            return;
        _history.PushBeforeChange(_elements);
        _elements.Clear();
        _selection.Clear();
        Notify();
    }

    /// <summary>Sets selection to the given ids (existing elements only).</summary>
    public void SetSelection(IEnumerable<string>? ids)
    {
        _selection.Clear();
        if (ids is not null)
        {
            foreach (var id in ids)
            {
                if (Find(id) is not null)
                    _selection.Add(id);
            }
        }

        Notify();
    }

    /// <summary>Selects a single element (or clears if null).</summary>
    public void Select(string? id)
    {
        _selection.Clear();
        if (id is not null && Find(id) is not null)
            _selection.Add(id);
        Notify();
    }

    /// <summary>Adds <paramref name="id"/> to the selection if it exists.</summary>
    public void AddToSelection(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (Find(id) is null)
            return;
        if (_selection.Add(id))
            Notify();
    }

    /// <summary>Toggles <paramref name="id"/> in the selection.</summary>
    public void ToggleSelection(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (Find(id) is null)
            return;
        if (!_selection.Remove(id))
            _selection.Add(id);
        Notify();
    }

    /// <summary>
    /// Records history then invokes <paramref name="mutate"/> for in-place geometry edits
    /// (move / resize / gridify).
    /// </summary>
    public void Mutate(Action mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);
        _history.PushBeforeChange(_elements);
        mutate();
        Notify();
    }

    /// <summary>
    /// Pushes the current element list onto the undo stack without mutating
    /// (e.g. before a multi-frame drag that edits points in place).
    /// </summary>
    public void Checkpoint() => _history.PushBeforeChange(_elements);

    /// <summary>
    /// Gridifies selected strokes (or all strokes if selection is empty)
    /// using the current <see cref="GridSettings.Size"/>.
    /// </summary>
    public void GridifySelection()
    {
        var targets = _selection.Count > 0
            ? _elements.Where(e => _selection.Contains(e.Id)).ToList()
            : _elements.ToList();
        if (targets.Count == 0)
            return;

        var g = Grid.Size;
        Mutate(() =>
        {
            foreach (var stroke in targets)
                stroke.Points = SketchGridify.Gridify(stroke.Points, g);
        });
    }

    /// <summary>Undoes the last mutating operation.</summary>
    public bool Undo()
    {
        var prior = _history.PopUndo(_elements);
        if (prior is null)
            return false;
        _elements.Clear();
        foreach (var e in prior)
            _elements.Add(e);
        PruneSelection();
        Notify();
        return true;
    }

    /// <summary>Redoes the last undone operation.</summary>
    public bool Redo()
    {
        var next = _history.PopRedo(_elements);
        if (next is null)
            return false;
        _elements.Clear();
        foreach (var e in next)
            _elements.Add(e);
        PruneSelection();
        Notify();
        return true;
    }

    /// <summary>Raises <see cref="Changed"/> without mutating (e.g. after grid property tweaks).</summary>
    public void NotifyChanged() => Notify();

    void PruneSelection()
    {
        if (_selection.Count == 0)
            return;
        _selection.RemoveWhere(id => Find(id) is null);
    }

    void Notify() => Changed?.Invoke();
}
