using Novolis.Avalonia.Ship.Design.Session;
using Novolis.Ship.Design;

namespace Novolis.Avalonia.Ship.Design.Plan;

/// <summary>PLAN stroke tools: wall / room / opening / passage / select.</summary>
public sealed class ShipArchitectToolController
{
    private readonly ShipDesignSession _session;
    private readonly List<float[]> _stroke = [];
    private string? _status;

    public ShipArchitectToolController(ShipDesignSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public string? Status => _status;

    public IReadOnlyList<float[]> StrokePoints => _stroke;

    public void Cancel()
    {
        _stroke.Clear();
        _status = null;
    }

    public void OnToolChanged()
    {
        _stroke.Clear();
        _status = Hint(_session.ActiveTool);
    }

    /// <summary>Returns true when the click was consumed by a tool.</summary>
    public bool OnLeftClick(float x, float z, bool finishStroke)
    {
        if (!_session.HasShip || _session.Workspace != ShipWorkspaceKind.Plan)
            return false;
        if (_session.Design.Decks.Count == 0)
            return false;

        var deck = _session.Design.Decks[
            System.Math.Clamp(_session.ActiveDeckIndex, 0, _session.Design.Decks.Count - 1)];
        x = _session.Snap(x);
        z = _session.Snap(z);

        return _session.ActiveTool switch
        {
            ShipDesignTool.Select => SelectAt(x, z, deck.Id),
            ShipDesignTool.Bulkhead => WallClick(x, z, deck, finishStroke),
            ShipDesignTool.Compartment => RoomClick(x, z, deck, finishStroke),
            ShipDesignTool.Passage => PassageClick(x, z, deck, finishStroke),
            ShipDesignTool.Opening => OpeningClick(x, z, deck.Id),
            ShipDesignTool.Equipment => EquipmentClick(x, z, deck),
            _ => false,
        };
    }

    public bool OnKeyFinish()
    {
        if (_stroke.Count < 2)
            return false;
        if (!_session.HasShip || _session.Design.Decks.Count == 0)
            return false;
        var deck = _session.Design.Decks[
            System.Math.Clamp(_session.ActiveDeckIndex, 0, _session.Design.Decks.Count - 1)];
        return _session.ActiveTool switch
        {
            ShipDesignTool.Bulkhead => FinishWall(deck),
            ShipDesignTool.Compartment => FinishRoom(deck),
            ShipDesignTool.Passage => FinishPassage(deck),
            _ => false,
        };
    }

    private bool SelectAt(float x, float z, DeckId deckId)
    {
        const float tol = 0.6f;
        if (ShipPlanPaths.TryHitBulkhead(_session.Design, deckId, x, z, tol, out var bh, out _) && bh is not null)
        {
            _session.Select(bh.Id.AsObject());
            _status = $"Selected {bh.Name}";
            return true;
        }

        foreach (var c in _session.Design.Compartments.Where(c => c.DeckId.Value == deckId.Value))
        {
            if (PointInPolygon(x, z, ShipPlanPaths.ExtractPolygonXz(c.Geometry)))
            {
                _session.Select(c.Id.AsObject());
                _status = $"Selected {c.Name}";
                return true;
            }
        }

        foreach (var p in _session.Design.Passages.Where(p => p.DeckId.Value == deckId.Value))
        {
            var path = ShipPlanPaths.ExtractPathXz(p.Geometry);
            for (var i = 0; i < path.Count - 1; i++)
            {
                if (ShipPlanPaths.DistancePointToSegment(x, z, path[i][0], path[i][1], path[i + 1][0], path[i + 1][1])
                    <= ShipLengths.ToMeters(p.Width) * 0.5f + tol)
                {
                    _session.Select(p.Id.AsObject());
                    _status = $"Selected {p.Name}";
                    return true;
                }
            }
        }

        foreach (var e in _session.Design.Equipment)
        {
            var path = ShipPlanPaths.ExtractPathXz(e.Geometry);
            if (path.Count >= 2)
            {
                var minX = path.Min(p => p[0]);
                var maxX = path.Max(p => p[0]);
                var minZ = path.Min(p => p[1]);
                var maxZ = path.Max(p => p[1]);
                if (x >= minX && x <= maxX && z >= minZ && z <= maxZ)
                {
                    _session.Select(e.Id.AsObject());
                    _status = $"Selected {e.Name}";
                    return true;
                }
            }
        }

        _session.Select(null);
        _status = "Select — click a wall, room, passage, or equipment";
        return true;
    }

    private bool WallClick(float x, float z, DeckDesign deck, bool finish)
    {
        _stroke.Add([x, z]);
        if (finish && _stroke.Count >= 2)
            return FinishWall(deck);
        _status = _stroke.Count < 2
            ? "Wall — click next point (Enter or double-click to finish)"
            : $"Wall — {_stroke.Count} pts · Enter/double-click to finish";
        _session.NotifyStrokePreview();
        return true;
    }

    private bool FinishWall(DeckDesign deck)
    {
        if (_stroke.Count < 2)
            return false;
        var pts = _stroke.ToList();
        _stroke.Clear();
        var thick = System.Math.Max(0.05f, _session.Design.Ship.HullThicknessMeters);
        var h = System.Math.Max(2.2f, _session.Design.Ship.HeightMeters / System.Math.Max(1, _session.Design.Ship.DeckCount) * 0.9f);
        _session.PushUndo();
        _session.Mutate(d => ShipDesignMutations.AddBulkheadPath(
            d, deck.Id, $"Wall {d.Bulkheads.Count + 1}", pts, thick, h));
        var last = _session.Design.Bulkheads[^1];
        _session.Select(last.Id.AsObject());
        _status = $"Placed {last.Name}";
        return true;
    }

    private bool RoomClick(float x, float z, DeckDesign deck, bool finish)
    {
        _stroke.Add([x, z]);
        if (finish && _stroke.Count >= 3)
            return FinishRoom(deck);
        _status = _stroke.Count < 3
            ? "Room — click corners (≥3), Enter/double-click to close"
            : $"Room — {_stroke.Count} pts · Enter/double-click to close";
        _session.NotifyStrokePreview();
        return true;
    }

    private bool FinishRoom(DeckDesign deck)
    {
        if (_stroke.Count < 3)
            return false;
        var pts = _stroke.ToList();
        _stroke.Clear();
        _session.PushUndo();
        _session.Mutate(d => ShipDesignMutations.AddCompartmentPolygon(
            d, deck.Id, $"Room {d.Compartments.Count + 1}", pts));
        var last = _session.Design.Compartments[^1];
        _session.Select(last.Id.AsObject());
        _status = $"Placed {last.Name}";
        return true;
    }

    private bool PassageClick(float x, float z, DeckDesign deck, bool finish)
    {
        _stroke.Add([x, z]);
        if (finish && _stroke.Count >= 2)
            return FinishPassage(deck);
        _status = _stroke.Count < 2
            ? "Passage — click path (Enter/double-click to finish)"
            : $"Passage — {_stroke.Count} pts · Enter/double-click to finish";
        _session.NotifyStrokePreview();
        return true;
    }

    private bool FinishPassage(DeckDesign deck)
    {
        if (_stroke.Count < 2)
            return false;
        var pts = _stroke.ToList();
        _stroke.Clear();
        _session.PushUndo();
        _session.Mutate(d => ShipDesignMutations.AddPassage(
            d, deck.Id, $"Passage {d.Passages.Count + 1}", pts, 1.2f, 2.2f));
        var last = _session.Design.Passages[^1];
        _session.Select(last.Id.AsObject());
        _status = $"Placed {last.Name}";
        return true;
    }

    private bool OpeningClick(float x, float z, DeckId deckId)
    {
        if (!ShipPlanPaths.TryHitBulkhead(_session.Design, deckId, x, z, 0.8f, out var host, out var t) || host is null)
        {
            _status = "Opening — click on a bulkhead/wall";
            return true;
        }

        _session.PushUndo();
        _session.Mutate(d => ShipDesignMutations.AddOpeningOnHost(
            d, host.Id, $"Door {d.Openings.Count + 1}", OpeningKind.Door, t, 0.9f, 2.0f));
        var last = _session.Design.Openings[^1];
        _session.Select(last.Id.AsObject());
        _status = $"Placed {last.Name} on {host.Name}";
        return true;
    }

    private bool EquipmentClick(float x, float z, DeckDesign deck)
    {
        var elev = ShipLengths.ToMeters(deck.Elevation);
        _session.PushUndo();
        _session.Mutate(d => ShipDesignMutations.AddEquipment(
            d, $"Equipment {d.Equipment.Count + 1}",
            [x, elev + 1f, z], [1f, 1f, 1.5f], 800f, deck.Index));
        var last = _session.Design.Equipment[^1];
        _session.Select(last.Id.AsObject());
        _status = $"Placed {last.Name}";
        return true;
    }

    public static string Hint(ShipDesignTool tool) => tool switch
    {
        ShipDesignTool.Select => "Select — click walls, rooms, passages",
        ShipDesignTool.Bulkhead => "Wall — click segment points, Enter to finish",
        ShipDesignTool.Compartment => "Room — click polygon corners, Enter to close",
        ShipDesignTool.Passage => "Passage — click path, Enter to finish",
        ShipDesignTool.Opening => "Opening — click a wall to place a door",
        ShipDesignTool.Equipment => "Equipment — click to place envelope",
        ShipDesignTool.Hull => "Hull selected",
        ShipDesignTool.Structure => "Structure selected",
        _ => tool.ToString(),
    };

    private static bool PointInPolygon(float x, float z, IReadOnlyList<float[]> poly)
    {
        if (poly.Count < 3)
            return false;
        var inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            var xi = poly[i][0];
            var zi = poly[i][1];
            var xj = poly[j][0];
            var zj = poly[j][1];
            var intersect = ((zi > z) != (zj > z))
                            && (x < (xj - xi) * (z - zi) / ((zj - zi) + 1e-12f) + xi);
            if (intersect)
                inside = !inside;
        }

        return inside;
    }
}
