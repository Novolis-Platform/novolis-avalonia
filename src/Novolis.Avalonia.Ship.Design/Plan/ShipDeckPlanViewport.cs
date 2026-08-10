using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Novolis.Avalonia.Ship.Design.Grips;
using Novolis.Avalonia.Ship.Design.Session;
using Novolis.Ship.Design;
using System.Numerics;

namespace Novolis.Avalonia.Ship.Design.Plan;

/// <summary>Ship-native XZ deck plan — semantic authoring surface (not CAD IsolateLevel).</summary>
public sealed class ShipDeckPlanViewport : Control
{
    private readonly ShipDesignSession _session;
    private readonly ShipArchitectToolController _tools;
    private readonly IBrush _canvas = new SolidColorBrush(Color.FromRgb(22, 24, 28));
    private double _scale = 18;
    private double _originX;
    private double _originZ;
    private bool _panning;
    private Point _last;
    private Point? _hover;
    private DateTime _lastClickUtc;
    private ShipGrip? _dragGrip;
    private ShipPlanConstraintResult? _hoverConstraint;
    private KeyModifiers _mods;

    public ShipDeckPlanViewport(ShipDesignSession session, ShipArchitectToolController tools)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _tools = tools ?? throw new ArgumentNullException(nameof(tools));
        Focusable = true;
        ClipToBounds = true;
        _session.Changed += () => InvalidateVisual();
        _session.StrokePreviewChanged += () => InvalidateVisual();
    }

    public ShipArchitectToolController Tools => _tools;

    public void Fit()
    {
        if (!_session.HasShip)
        {
            _scale = 18;
            _originX = 0;
            _originZ = 0;
            InvalidateVisual();
            return;
        }

        var L = _session.Design.Ship.LengthMeters;
        var B = _session.Design.Ship.BeamMeters;
        var pad = 1.15;
        var sx = Bounds.Width > 40 ? Bounds.Width / (B * pad) : 18;
        var sz = Bounds.Height > 40 ? Bounds.Height / (L * pad) : 18;
        _scale = System.Math.Clamp(System.Math.Min(sx, sz), 4, 120);
        _originX = 0;
        _originZ = 0;
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        _mods = e.KeyModifiers;
        var p = e.GetPosition(this);
        var props = e.GetCurrentPoint(this).Properties;
        if (props.IsMiddleButtonPressed)
        {
            _panning = true;
            _last = p;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (!props.IsLeftButtonPressed)
            return;

        var resolved = ResolveAt(ScreenToWorld(p), preferEdge: _session.ActiveTool == ShipDesignTool.Opening);
        _hoverConstraint = resolved;
        _session.LastSnapKind = resolved.Kind;
        var world = new Vector3(resolved.X, 0, resolved.Z);
        if (_session.ActiveTool == ShipDesignTool.Select && TryBeginGrip(world))
        {
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        var now = DateTime.UtcNow;
        var dbl = (now - _lastClickUtc).TotalMilliseconds < 320;
        _lastClickUtc = now;
        if (_tools.OnLeftClick(world.X, world.Z, finishStroke: dbl))
        {
            if (_tools.Status is { } s)
                _session.SetStatusMessage(FormatStatus(s, resolved));
            InvalidateVisual();
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        _mods = e.KeyModifiers;
        var p = e.GetPosition(this);
        _hover = p;
        if (_panning)
        {
            _originX -= (p.X - _last.X) / _scale;
            _originZ -= (p.Y - _last.Y) / _scale;
            _last = p;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        var resolved = ResolveAt(ScreenToWorld(p), preferEdge: _session.ActiveTool == ShipDesignTool.Opening);
        _hoverConstraint = resolved;
        _session.LastSnapKind = resolved.Kind;

        if (_dragGrip is not null && _session.SelectedObjectId is { } oid)
        {
            ApplyGrip(oid, _dragGrip, new Vector3(resolved.X, 0, resolved.Z));
            e.Handled = true;
            return;
        }

        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_panning)
        {
            _panning = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }

        if (_dragGrip is not null)
        {
            _dragGrip = null;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var factor = e.Delta.Y > 0 ? 1.12 : 0.89;
        var before = ScreenToWorld(e.GetPosition(this));
        _scale = System.Math.Clamp(_scale * factor, 2, 200);
        var after = ScreenToWorld(e.GetPosition(this));
        _originX += before.X - after.X;
        _originZ += before.Z - after.Z;
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        _mods = e.KeyModifiers;
        if (e.Key == Key.F8)
        {
            _session.SetOrthoLocked(!_session.OrthoLocked);
            _session.SetStatusMessage(_session.OrthoLocked ? "ORTHO on (F8)" : "ORTHO off");
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F7)
        {
            _session.SetAngleLockEnabled(!_session.AngleLockEnabled);
            _session.SetStatusMessage(_session.AngleLockEnabled ? "ANG15 on (F7)" : "ANG15 off");
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (e.Key is Key.Enter or Key.Space)
        {
            if (_tools.OnKeyFinish())
            {
                if (_tools.Status is { } s)
                    _session.SetStatusMessage(s);
                InvalidateVisual();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Escape)
        {
            _tools.Cancel();
            _session.SetStatusMessage(ShipArchitectToolController.Hint(_session.ActiveTool));
            InvalidateVisual();
            e.Handled = true;
        }
        else if (e.Key == Key.Z && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (_session.TryUndo())
            {
                _session.SetStatusMessage("Undo");
                e.Handled = true;
            }
        }
    }

    public override void Render(DrawingContext context)
    {
        context.FillRectangle(_canvas, new Rect(Bounds.Size));
        DrawGrid(context);
        if (!_session.HasShip)
        {
            DrawBadge(context, "Create a ship to start drawing on the deck plan");
            return;
        }

        var design = _session.Design;
        var deck = design.Decks.Count == 0
            ? null
            : design.Decks[System.Math.Clamp(_session.ActiveDeckIndex, 0, design.Decks.Count - 1)];

        DrawHullFootprint(context, design);
        if (_session.ShowStructuralOverlays)
        {
            DrawFrames(context, design);
            DrawCutoutHosts(context, design, deck);
        }

        if (deck is not null)
        {
            foreach (var c in design.Compartments.Where(c => c.DeckId.Value == deck.Id.Value))
                DrawCompartment(context, c, IsSelected(c.Id.AsObject()), IsHighlighted(c.Id.AsObject()));
            foreach (var b in design.Bulkheads.Where(b => b.DeckId?.Value == deck.Id.Value || b.IsPrimary))
                DrawBulkhead(context, b, IsSelected(b.Id.AsObject()), IsHighlighted(b.Id.AsObject()));
            foreach (var p in design.Passages.Where(p => p.DeckId.Value == deck.Id.Value))
                DrawPassage(context, p, IsSelected(p.Id.AsObject()), IsHighlighted(p.Id.AsObject()));
            foreach (var o in design.Openings)
                DrawOpening(context, design, o, IsSelected(o.Id.AsObject()), IsHighlighted(o.Id.AsObject()));
            foreach (var eq in design.Equipment)
                DrawEquipment(context, eq, IsSelected(eq.Id.AsObject()), IsHighlighted(eq.Id.AsObject()));
        }

        DrawStroke(context);
        DrawGuides(context);
        if (_session.ShowDimensions && _session.SelectedObjectId is { } sel)
            DrawDims(context, design, sel);
        DrawGrips(context);
        DrawBadge(context, deck is null
            ? design.Ship.Name
            : $"{deck.Name} · {ConstraintBadge()} · {ShipArchitectToolController.Hint(_session.ActiveTool)}");
        DrawHover(context);
    }

    private bool IsSelected(ShipObjectId id) =>
        _session.SelectedObjectId is { } s && s.Value == id.Value;

    private bool IsHighlighted(ShipObjectId id) =>
        _session.HighlightedObjectIds.Any(h => h.Value == id.Value);

    private void DrawHullFootprint(DrawingContext context, ShipDesign design)
    {
        var L = design.Ship.LengthMeters;
        var B = design.Ship.BeamMeters;
        var pen = new Pen(new SolidColorBrush(Color.FromRgb(160, 175, 195)), 2);
        DrawRect(context, pen, -B * 0.5f, -L * 0.5f, B * 0.5f, L * 0.5f);
    }

    private void DrawFrames(DrawingContext context, ShipDesign design)
    {
        var B = design.Ship.BeamMeters;
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(80, 120, 140, 160)), 1);
        foreach (var f in design.Frames)
        {
            var z = ShipLengths.ToMeters(f.Station);
            context.DrawLine(pen, WorldToScreen(new Vector3(-B * 0.45f, 0, z)), WorldToScreen(new Vector3(B * 0.45f, 0, z)));
        }
    }

    private void DrawCutoutHosts(DrawingContext context, ShipDesign design, DeckDesign? deck)
    {
        if (deck is null || design.Cutouts.Count == 0)
            return;
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(160, 220, 140, 60)), 2, dashStyle: DashStyle.Dash);
        foreach (var cut in design.Cutouts)
        {
            var host = design.Bulkheads.FirstOrDefault(b => b.Id.Value == cut.HostId.Value);
            if (host is null)
                continue;
            if (host.DeckId is { } hid && hid.Value != deck.Id.Value && !host.IsPrimary)
                continue;
            var path = ShipPlanPaths.ExtractPathXz(host.Geometry);
            if (path.Count < 2)
                continue;
            for (var i = 0; i < path.Count - 1; i++)
            {
                context.DrawLine(
                    pen,
                    WorldToScreen(new Vector3(path[i][0], 0, path[i][1])),
                    WorldToScreen(new Vector3(path[i + 1][0], 0, path[i + 1][1])));
            }
        }
    }

    private void DrawBulkhead(DrawingContext context, BulkheadDesign b, bool selected, bool highlighted)
    {
        var path = ShipPlanPaths.ExtractPathXz(b.Geometry);
        if (path.Count < 2)
            return;
        var color = highlighted ? Color.FromRgb(240, 180, 60)
            : selected ? Color.FromRgb(90, 200, 255)
            : b.IsPrimary ? Color.FromRgb(200, 200, 210) : Color.FromRgb(170, 190, 210);
        var pen = new Pen(new SolidColorBrush(color), selected || highlighted ? 4 : System.Math.Max(2, ShipLengths.ToMeters(b.Thickness) * _scale * 0.35));
        for (var i = 0; i < path.Count - 1; i++)
            context.DrawLine(pen, WorldToScreen(new Vector3(path[i][0], 0, path[i][1])), WorldToScreen(new Vector3(path[i + 1][0], 0, path[i + 1][1])));
    }

    private void DrawCompartment(DrawingContext context, CompartmentDesign c, bool selected, bool highlighted)
    {
        var poly = ShipPlanPaths.ExtractPolygonXz(c.Geometry);
        if (poly.Count < 3)
            return;
        var fill = highlighted ? Color.FromArgb(70, 240, 180, 60)
            : selected ? Color.FromArgb(60, 80, 160, 220)
            : Color.FromArgb(40, 70, 120, 90);
        var pen = new Pen(new SolidColorBrush(selected || highlighted ? Colors.White : Color.FromRgb(100, 160, 120)), selected ? 2 : 1);
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            ctx.BeginFigure(WorldToScreen(new Vector3(poly[0][0], 0, poly[0][1])), isFilled: true);
            for (var i = 1; i < poly.Count; i++)
                ctx.LineTo(WorldToScreen(new Vector3(poly[i][0], 0, poly[i][1])));
            ctx.EndFigure(true);
        }

        context.DrawGeometry(new SolidColorBrush(fill), pen, geo);
    }

    private void DrawPassage(DrawingContext context, PassageDesign p, bool selected, bool highlighted)
    {
        var path = ShipPlanPaths.ExtractPathXz(p.Geometry);
        if (path.Count < 2)
            return;
        var color = highlighted ? Color.FromRgb(240, 180, 60)
            : selected ? Color.FromRgb(120, 220, 255) : Color.FromRgb(100, 180, 200);
        var pen = new Pen(new SolidColorBrush(color), selected || highlighted ? 3 : 2, dashStyle: new DashStyle([4, 3], 0));
        for (var i = 0; i < path.Count - 1; i++)
            context.DrawLine(pen, WorldToScreen(new Vector3(path[i][0], 0, path[i][1])), WorldToScreen(new Vector3(path[i + 1][0], 0, path[i + 1][1])));
    }

    private void DrawOpening(DrawingContext context, ShipDesign design, OpeningDesign o, bool selected, bool highlighted)
    {
        var host = design.Bulkheads.FirstOrDefault(b => b.Id.Value == o.HostId.Value);
        if (host is null)
            return;
        var path = ShipPlanPaths.ExtractPathXz(host.Geometry);
        if (path.Count < 2)
            return;
        var center = o.Geometry.Entities.FirstOrDefault()?.Center;
        float x, z;
        if (center is { Length: >= 3 })
        {
            x = center[0];
            z = center[2];
        }
        else
        {
            var mid = ShipPlanPaths.PointAlong(path, 0.5f);
            x = mid[0];
            z = mid[1];
        }

        var color = highlighted ? Color.FromRgb(240, 180, 60)
            : selected ? Color.FromRgb(255, 200, 120) : Color.FromRgb(220, 160, 80);
        var pen = new Pen(new SolidColorBrush(color), selected || highlighted ? 3 : 2);
        var s = WorldToScreen(new Vector3(x, 0, z));
        context.DrawLine(pen, new Point(s.X - 6, s.Y), new Point(s.X + 6, s.Y));
        context.DrawLine(pen, new Point(s.X, s.Y - 6), new Point(s.X, s.Y + 6));
        context.DrawEllipse(null, pen, s, 5, 5);
    }

    private void DrawEquipment(DrawingContext context, EquipmentDesign eq, bool selected, bool highlighted)
    {
        var path = ShipPlanPaths.ExtractPathXz(eq.Geometry);
        if (path.Count < 2)
            return;
        var minX = path.Min(p => p[0]);
        var maxX = path.Max(p => p[0]);
        var minZ = path.Min(p => p[1]);
        var maxZ = path.Max(p => p[1]);
        var pen = new Pen(new SolidColorBrush(highlighted ? Color.FromRgb(240, 180, 60)
            : selected ? Color.FromRgb(200, 160, 255) : Color.FromRgb(160, 140, 200)), selected ? 2.5 : 1.5);
        DrawRect(context, pen, minX, minZ, maxX, maxZ);
    }

    private void DrawStroke(DrawingContext context)
    {
        var pts = _tools.StrokePoints;
        var pen = new Pen(new SolidColorBrush(Color.FromRgb(90, 220, 180)), 2);
        if (pts.Count >= 2)
        {
            for (var i = 0; i < pts.Count - 1; i++)
                context.DrawLine(pen, WorldToScreen(new Vector3(pts[i][0], 0, pts[i][1])), WorldToScreen(new Vector3(pts[i + 1][0], 0, pts[i + 1][1])));
        }

        foreach (var p in pts)
        {
            var s = WorldToScreen(new Vector3(p[0], 0, p[1]));
            context.DrawEllipse(new SolidColorBrush(Color.FromRgb(90, 220, 180)), null, s, 3.5, 3.5);
        }

        // Rubber-band to constrained hover.
        if (pts.Count > 0 && _hoverConstraint is { } hc
            && _session.ActiveTool is ShipDesignTool.Bulkhead or ShipDesignTool.Compartment or ShipDesignTool.Passage)
        {
            var last = pts[^1];
            var rubber = new Pen(new SolidColorBrush(Color.FromArgb(200, 120, 230, 200)), 1.5);
            context.DrawLine(
                rubber,
                WorldToScreen(new Vector3(last[0], 0, last[1])),
                WorldToScreen(new Vector3(hc.X, 0, hc.Z)));
            if (hc.SegmentLengthM is { } len && len > 1e-3f)
            {
                var mid = WorldToScreen(new Vector3((last[0] + hc.X) * 0.5f, 0, (last[1] + hc.Z) * 0.5f));
                var ang = hc.SegmentAngleDeg ?? 0f;
                DrawLabel(context, mid, $"{len:0.##} m · {ang:0.#}°");
            }
        }
    }

    private void DrawGuides(DrawingContext context)
    {
        if (_hoverConstraint is not { Guides.Count: > 0 } hc)
            return;
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(140, 255, 180, 70)), 1, dashStyle: DashStyle.Dash);
        foreach (var g in hc.Guides)
        {
            context.DrawLine(
                pen,
                WorldToScreen(new Vector3(g.Ax, 0, g.Az)),
                WorldToScreen(new Vector3(g.Bx, 0, g.Bz)));
        }

        // Snap marker
        var s = WorldToScreen(new Vector3(hc.X, 0, hc.Z));
        var mark = hc.Kind switch
        {
            ShipPlanConstraintSnapKind.Vertex => Color.FromRgb(255, 210, 90),
            ShipPlanConstraintSnapKind.Midpoint => Color.FromRgb(120, 200, 255),
            ShipPlanConstraintSnapKind.Edge => Color.FromRgb(180, 220, 140),
            _ => Color.FromRgb(160, 160, 170),
        };
        context.DrawEllipse(new SolidColorBrush(mark), new Pen(Brushes.Black, 1), s, 4.5, 4.5);
    }

    private void DrawDims(DrawingContext context, ShipDesign design, ShipObjectId id)
    {
        var bh = design.Bulkheads.FirstOrDefault(b => b.Id.Value == id.Value);
        if (bh is not null)
        {
            var path = ShipPlanPaths.ExtractPathXz(bh.Geometry);
            if (path.Count >= 2)
            {
                var len = 0f;
                for (var i = 0; i < path.Count - 1; i++)
                {
                    var dx = path[i + 1][0] - path[i][0];
                    var dz = path[i + 1][1] - path[i][1];
                    len += MathF.Sqrt(dx * dx + dz * dz);
                }

                var mid = ShipPlanPaths.PointAlong(path, 0.5f);
                DrawLabel(context, WorldToScreen(new Vector3(mid[0], 0, mid[1])), $"L={len:0.##} m");
            }
        }

        var room = design.Compartments.FirstOrDefault(c => c.Id.Value == id.Value);
        if (room is not null)
        {
            var poly = ShipPlanPaths.ExtractPolygonXz(room.Geometry);
            if (poly.Count >= 3)
            {
                var area = PolygonArea(poly);
                var cx = poly.Average(p => p[0]);
                var cz = poly.Average(p => p[1]);
                DrawLabel(context, WorldToScreen(new Vector3(cx, 0, cz)), $"A={area:0.#} m²");
            }
        }
    }

    private void DrawGrips(DrawingContext context)
    {
        if (_session.SelectedObjectId is null || _session.ActiveTool != ShipDesignTool.Select)
            return;
        foreach (var g in ShipGripCatalog.ForSelection(_session.Design, _session.SelectedObjectId))
        {
            if (g.Kind is ShipGripKind.Elevation or ShipGripKind.Station)
                continue;
            var s = WorldToScreen(new Vector3(g.X, 0, g.Z));
            context.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 210, 90)), new Pen(Brushes.Black, 1), s, 5, 5);
        }
    }

    private bool TryBeginGrip(Vector3 world)
    {
        if (_session.SelectedObjectId is null)
            return false;
        foreach (var g in ShipGripCatalog.ForSelection(_session.Design, _session.SelectedObjectId))
        {
            if (g.Kind is ShipGripKind.Elevation or ShipGripKind.Station)
                continue;
            var dx = g.X - world.X;
            var dz = g.Z - world.Z;
            if (MathF.Sqrt(dx * dx + dz * dz) <= 0.45f)
            {
                _dragGrip = g;
                _session.PushUndo();
                return true;
            }
        }

        return false;
    }

    private void ApplyGrip(ShipObjectId oid, ShipGrip grip, Vector3 world)
    {
        var bh = _session.Design.Bulkheads.FirstOrDefault(b => b.Id.Value == oid.Value);
        if (bh is not null && grip.Kind is ShipGripKind.Endpoint or ShipGripKind.Vertex)
        {
            var path = ShipPlanPaths.ExtractPathXz(bh.Geometry);
            var idx = NearestIndex(path, grip.X, grip.Z);
            if (idx >= 0)
            {
                path[idx] = [world.X, world.Z];
                _session.Mutate(d => ShipDesignMutations.UpdateBulkheadPath(d, bh.Id, path));
            }

            return;
        }

        var room = _session.Design.Compartments.FirstOrDefault(c => c.Id.Value == oid.Value);
        if (room is not null && grip.Kind is ShipGripKind.Vertex or ShipGripKind.Endpoint)
        {
            var poly = ShipPlanPaths.ExtractPathXz(room.Geometry);
            var idx = NearestIndex(poly, grip.X, grip.Z);
            if (idx >= 0)
            {
                poly[idx] = [world.X, world.Z];
                _session.Mutate(d => ShipDesignMutations.UpdateCompartmentPolygon(d, room.Id, poly));
            }

            return;
        }

        var passage = _session.Design.Passages.FirstOrDefault(p => p.Id.Value == oid.Value);
        if (passage is not null)
        {
            if (grip.Kind is ShipGripKind.Endpoint or ShipGripKind.Vertex)
            {
                var path = ShipPlanPaths.ExtractPathXz(passage.Geometry);
                var idx = NearestIndex(path, grip.X, grip.Z);
                if (idx >= 0)
                {
                    path[idx] = [world.X, world.Z];
                    _session.Mutate(d => ShipDesignMutations.UpdatePassagePath(d, passage.Id, path));
                }

                return;
            }

            if (grip.Kind == ShipGripKind.Width)
            {
                var path = ShipPlanPaths.ExtractPathXz(passage.Geometry);
                var t = ShipPlanPaths.NearestParameter(path, world.X, world.Z);
                var on = ShipPlanPaths.PointAlong(path, t);
                var dist = MathF.Sqrt((world.X - on[0]) * (world.X - on[0]) + (world.Z - on[1]) * (world.Z - on[1]));
                _session.Mutate(d => ShipDesignMutations.SetPassageWidth(d, passage.Id, System.Math.Max(0.6f, dist * 2f)));
                return;
            }
        }

        var opening = _session.Design.Openings.FirstOrDefault(o => o.Id.Value == oid.Value);
        if (opening is not null && grip.Kind == ShipGripKind.Slide)
        {
            var host = _session.Design.Bulkheads.FirstOrDefault(b => b.Id.Value == opening.HostId.Value);
            if (host is null)
                return;
            var path = ShipPlanPaths.ExtractPathXz(host.Geometry);
            var t = ShipPlanPaths.NearestParameter(path, world.X, world.Z);
            _session.Mutate(d => ShipDesignMutations.MoveOpeningAlongHost(d, opening.Id, t));
        }
    }

    private static int NearestIndex(IReadOnlyList<float[]> path, float x, float z)
    {
        var best = -1;
        var bestD = float.MaxValue;
        for (var i = 0; i < path.Count; i++)
        {
            var dx = path[i][0] - x;
            var dz = path[i][1] - z;
            var d = dx * dx + dz * dz;
            if (d < bestD)
            {
                bestD = d;
                best = i;
            }
        }

        return best;
    }

    private void DrawGrid(DrawingContext context)
    {
        var step = System.Math.Max(0.25f, _session.SnapGridMeters);
        var topLeft = ScreenToWorld(new Point(0, 0));
        var bottomRight = ScreenToWorld(new Point(Bounds.Width, Bounds.Height));
        var minX = System.Math.Min(topLeft.X, bottomRight.X);
        var maxX = System.Math.Max(topLeft.X, bottomRight.X);
        var minZ = System.Math.Min(topLeft.Z, bottomRight.Z);
        var maxZ = System.Math.Max(topLeft.Z, bottomRight.Z);
        var minor = new Pen(new SolidColorBrush(Color.FromRgb(38, 42, 48)), 1);
        var major = new Pen(new SolidColorBrush(Color.FromRgb(52, 58, 68)), 1);
        var axis = new Pen(new SolidColorBrush(Color.FromRgb(80, 110, 150)), 1.5);
        for (var x = MathF.Floor((float)minX / step) * step; x <= maxX + step; x += step)
        {
            var pen = MathF.Abs(x) < step * 0.01f ? axis : ((int)MathF.Round(x / step) % 5 == 0 ? major : minor);
            context.DrawLine(pen, WorldToScreen(new Vector3(x, 0, (float)minZ)), WorldToScreen(new Vector3(x, 0, (float)maxZ)));
        }

        for (var z = MathF.Floor((float)minZ / step) * step; z <= maxZ + step; z += step)
        {
            var pen = MathF.Abs(z) < step * 0.01f ? axis : ((int)MathF.Round(z / step) % 5 == 0 ? major : minor);
            context.DrawLine(pen, WorldToScreen(new Vector3((float)minX, 0, z)), WorldToScreen(new Vector3((float)maxX, 0, z)));
        }
    }

    private void DrawRect(DrawingContext context, IPen pen, float minX, float minZ, float maxX, float maxZ)
    {
        var a = WorldToScreen(new Vector3(minX, 0, minZ));
        var b = WorldToScreen(new Vector3(maxX, 0, minZ));
        var c = WorldToScreen(new Vector3(maxX, 0, maxZ));
        var d = WorldToScreen(new Vector3(minX, 0, maxZ));
        context.DrawLine(pen, a, b);
        context.DrawLine(pen, b, c);
        context.DrawLine(pen, c, d);
        context.DrawLine(pen, d, a);
    }

    private void DrawBadge(DrawingContext context, string text)
    {
        context.DrawText(
            new FormattedText(text, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 12, Brushes.LightGray),
            new Point(10, 8));
    }

    private void DrawLabel(DrawingContext context, Point at, string text)
    {
        context.DrawText(
            new FormattedText(text, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Consolas"), 11, Brushes.WhiteSmoke),
            new Point(at.X + 6, at.Y - 14));
    }

    private void DrawHover(DrawingContext context)
    {
        if (_hoverConstraint is not { } hc)
            return;
        var mode = ConstraintBadge();
        context.DrawText(
            new FormattedText(
                $"{hc.X:0.##}, {hc.Z:0.##} m · {hc.Kind} · {mode}",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, new Typeface("Consolas"), 11, Brushes.Gray),
            new Point(10, Bounds.Height - 22));
    }

    private ShipPlanConstraintResult ResolveAt(Vector3 raw, bool preferEdge)
    {
        DeckId? deckId = null;
        if (_session.HasShip && _session.Design.Decks.Count > 0)
        {
            var deck = _session.Design.Decks[
                System.Math.Clamp(_session.ActiveDeckIndex, 0, _session.Design.Decks.Count - 1)];
            deckId = deck.Id;
        }

        float? lastX = null;
        float? lastZ = null;
        if (_dragGrip is not null)
        {
            lastX = _dragGrip.X;
            lastZ = _dragGrip.Z;
        }
        else if (_tools.LastStrokePoint() is { } last)
        {
            lastX = last[0];
            lastZ = last[1];
        }

        // Screen-scaled object snap (~12 px).
        var tol = (float)System.Math.Clamp(12.0 / _scale, 0.2, 0.75);
        return ShipPlanConstraintResolver.Resolve(
            raw.X,
            raw.Z,
            lastX,
            lastZ,
            _session,
            deckId,
            altFree: _mods.HasFlag(KeyModifiers.Alt),
            shiftOrtho: _mods.HasFlag(KeyModifiers.Shift),
            ctrlAngle: _mods.HasFlag(KeyModifiers.Control),
            objectSnapTolM: tol,
            preferEdgeFirst: preferEdge);
    }

    private string ConstraintBadge()
    {
        if (_mods.HasFlag(KeyModifiers.Alt))
            return "FREE";
        if (_mods.HasFlag(KeyModifiers.Control) || _session.AngleLockEnabled)
            return "ANG15";
        if (_mods.HasFlag(KeyModifiers.Shift) || _session.OrthoLocked)
            return "ORTHO";
        return _session.SnapEnabled ? "SNAP" : "OFF";
    }

    private static string FormatStatus(string toolStatus, ShipPlanConstraintResult resolved)
    {
        var extra = resolved.SegmentLengthM is { } len && len > 1e-3f
            ? $" · {len:0.##} m"
            : "";
        return $"{toolStatus} · {resolved.Kind}{extra}";
    }

    private Point WorldToScreen(Vector3 w) =>
        new(Bounds.Width * 0.5 + (w.X - _originX) * _scale, Bounds.Height * 0.5 + (w.Z - _originZ) * _scale);

    private Vector3 ScreenToWorld(Point s) =>
        new((float)((s.X - Bounds.Width * 0.5) / _scale + _originX), 0,
            (float)((s.Y - Bounds.Height * 0.5) / _scale + _originZ));

    private static float PolygonArea(IReadOnlyList<float[]> poly)
    {
        double a = 0;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            a += (double)poly[j][0] * poly[i][1] - (double)poly[i][0] * poly[j][1];
        return (float)(System.Math.Abs(a) * 0.5);
    }
}
