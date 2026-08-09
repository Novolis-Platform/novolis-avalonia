namespace Novolis.Avalonia.Controls.Sketch;

/// <summary>In-memory sketch document: strokes, layers, selection, grid, and undo history.</summary>
public sealed class SketchDocument
{
    /// <summary>Id of the built-in default layer.</summary>
    public const string DefaultLayerId = "layer-default";

    readonly List<StrokeShape> _elements = [];
    readonly List<SketchLayer> _layers = [];
    readonly HashSet<string> _selection = new(StringComparer.Ordinal);
    readonly SketchHistory _history = new();

    /// <summary>Document format version (3 = layers).</summary>
    public int Version { get; set; } = 3;

    /// <summary>Grid configuration.</summary>
    public GridSettings Grid { get; } = new();

    /// <summary>Layer stack (bottom → top order matches list order for new docs).</summary>
    public IReadOnlyList<SketchLayer> Layers => _layers;

    /// <summary>Active layer for new elements.</summary>
    public string ActiveLayerId { get; set; } = DefaultLayerId;

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

    /// <summary>Creates a document with a single default layer.</summary>
    public SketchDocument()
    {
        EnsureDefaultLayer();
    }

    /// <summary>Ensures at least the default layer exists.</summary>
    public void EnsureDefaultLayer()
    {
        if (_layers.Count > 0)
            return;
        _layers.Add(new SketchLayer { Id = DefaultLayerId, Name = "Layer 1" });
        ActiveLayerId = DefaultLayerId;
    }

    /// <summary>Finds a layer by id.</summary>
    public SketchLayer? FindLayer(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            id = DefaultLayerId;
        foreach (var layer in _layers)
        {
            if (string.Equals(layer.Id, id, StringComparison.Ordinal))
                return layer;
        }

        return null;
    }

    /// <summary>Whether the layer (or default) is visible.</summary>
    public bool IsLayerVisible(string? layerId)
    {
        EnsureDefaultLayer();
        var layer = FindLayer(layerId) ?? FindLayer(DefaultLayerId);
        return layer?.Visible != false;
    }

    /// <summary>Whether the layer (or default) is locked.</summary>
    public bool IsLayerLocked(string? layerId)
    {
        EnsureDefaultLayer();
        var layer = FindLayer(layerId) ?? FindLayer(DefaultLayerId);
        return layer?.Locked == true;
    }

    /// <summary>Adds a layer and selects it as active.</summary>
    public SketchLayer AddLayer(string? name = null)
    {
        EnsureDefaultLayer();
        var layer = new SketchLayer
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = string.IsNullOrWhiteSpace(name) ? $"Layer {_layers.Count + 1}" : name.Trim()
        };
        _layers.Add(layer);
        ActiveLayerId = layer.Id;
        if (Version < 3)
            Version = 3;
        Notify();
        return layer;
    }

    /// <summary>Replaces the layer list (used by load).</summary>
    public void ReplaceLayers(IEnumerable<SketchLayer> layers)
    {
        ArgumentNullException.ThrowIfNull(layers);
        _layers.Clear();
        foreach (var layer in layers)
            _layers.Add(layer.Clone());
        EnsureDefaultLayer();
        if (FindLayer(ActiveLayerId) is null)
            ActiveLayerId = _layers[0].Id;
        Notify();
    }

    /// <summary>Toggles visibility of a layer.</summary>
    public void SetLayerVisible(string layerId, bool visible)
    {
        var layer = FindLayer(layerId);
        if (layer is null)
            return;
        layer.Visible = visible;
        Notify();
    }

    /// <summary>Toggles lock of a layer.</summary>
    public void SetLayerLocked(string layerId, bool locked)
    {
        var layer = FindLayer(layerId);
        if (layer is null)
            return;
        layer.Locked = locked;
        Notify();
    }

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
        EnsureDefaultLayer();
        if (string.IsNullOrWhiteSpace(stroke.LayerId))
            stroke.LayerId = ActiveLayerId;
        _history.PushBeforeChange(_elements);
        _elements.Add(stroke);
        Notify();
    }

    /// <summary>
    /// Applies fill to a closed (or nearly closed) stroke. Open freehand is refused —
    /// use flood-fill via the paint bucket for enclosed regions between strokes.
    /// </summary>
    public bool ApplyFill(string strokeId, string fillColor, double closeTolerance = 4)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(strokeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fillColor);
        var stroke = Find(strokeId);
        if (stroke is null)
            return false;
        if (IsLayerLocked(stroke.LayerId))
            return false;
        if (stroke.Kind != SketchElementKind.Stroke)
            return false;
        if (stroke.Points.Count < 3)
            return false;

        var nearly = NearlyClosed(stroke.Points, closeTolerance);
        if (!stroke.Closed && !nearly)
            return false;

        Mutate(() =>
        {
            stroke.FillColor = fillColor;
            stroke.Closed = true;
            if (!NearlyClosed(stroke.Points, 1e-6))
                stroke.Points.Add(stroke.Points[0]);
        });
        return true;
    }

    /// <summary>
    /// Flood-fills an enclosed empty region around <paramref name="seed"/> and adds a new
    /// closed filled polygon. Returns false when the region is open / unbounded.
    /// </summary>
    public bool TryFloodFill(SketchPoint seed, string fillColor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fillColor);
        if (IsLayerLocked(ActiveLayerId))
            return false;

        var contour = SketchFloodFill.TryCreateRegion(
            _elements,
            IsLayerVisible,
            seed);
        if (contour is null || contour.Count < 3)
            return false;

        AddStroke(new StrokeShape
        {
            Id = Guid.NewGuid().ToString("N"),
            Kind = SketchElementKind.Stroke,
            Points = [.. contour],
            StrokeColor = "#00000000",
            StrokeWidth = 0.25,
            FillColor = fillColor,
            StrokeStyle = SketchStrokeStyle.Solid,
            Closed = true,
            LayerId = ActiveLayerId
        });
        return true;
    }

    static bool NearlyClosed(IReadOnlyList<SketchPoint> pts, double tolerance)
    {
        if (pts.Count < 3)
            return false;
        var a = pts[0];
        var b = pts[^1];
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy) <= tolerance;
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
            var expanded = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in ids)
            {
                if (Find(id) is null)
                    continue;
                foreach (var member in GroupMemberIds(id))
                    expanded.Add(member);
            }

            foreach (var id in expanded)
                _selection.Add(id);
        }

        Notify();
    }

    /// <summary>Selects a single element (or clears if null). Expands to full group.</summary>
    public void Select(string? id)
    {
        _selection.Clear();
        if (id is not null && Find(id) is not null)
        {
            foreach (var member in GroupMemberIds(id))
                _selection.Add(member);
        }

        Notify();
    }

    /// <summary>Adds <paramref name="id"/> (and its group) to the selection if it exists.</summary>
    public void AddToSelection(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (Find(id) is null)
            return;
        var changed = false;
        foreach (var member in GroupMemberIds(id))
            changed |= _selection.Add(member);
        if (changed)
            Notify();
    }

    /// <summary>Toggles <paramref name="id"/> (and its group) in the selection.</summary>
    public void ToggleSelection(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (Find(id) is null)
            return;
        var members = GroupMemberIds(id).ToList();
        var allSelected = members.All(_selection.Contains);
        if (allSelected)
        {
            foreach (var m in members)
                _selection.Remove(m);
        }
        else
        {
            foreach (var m in members)
                _selection.Add(m);
        }

        Notify();
    }

    /// <summary>
    /// Fuses the current selection into one group (≥2 elements). Returns true when a group was created.
    /// </summary>
    public bool FuseSelection()
    {
        if (_selection.Count < 2)
            return false;

        var groupId = Guid.NewGuid().ToString("N");
        Mutate(() =>
        {
            foreach (var stroke in _elements)
            {
                if (_selection.Contains(stroke.Id))
                    stroke.GroupId = groupId;
            }
        });
        return true;
    }

    /// <summary>Clears <see cref="StrokeShape.GroupId"/> on the selection (full groups expanded).</summary>
    public bool UngroupSelection()
    {
        if (_selection.Count == 0)
            return false;

        var any = _selection.Any(id => Find(id)?.GroupId is not null);
        if (!any)
            return false;

        Mutate(() =>
        {
            foreach (var stroke in _elements)
            {
                if (_selection.Contains(stroke.Id))
                    stroke.GroupId = null;
            }
        });
        return true;
    }

    /// <summary>All element ids in the same group as <paramref name="id"/> (or just <paramref name="id"/>).</summary>
    public IEnumerable<string> GroupMemberIds(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var stroke = Find(id);
        if (stroke is null)
            yield break;

        if (string.IsNullOrWhiteSpace(stroke.GroupId))
        {
            yield return id;
            yield break;
        }

        var gid = stroke.GroupId;
        foreach (var e in _elements)
        {
            if (string.Equals(e.GroupId, gid, StringComparison.Ordinal))
                yield return e.Id;
        }
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
