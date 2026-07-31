using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Novolis.Avalonia.Cad.Commands;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Services;
using Novolis.Cad.Primitives;
using Novolis.Simulation.View;

namespace Novolis.Avalonia.Cad.Ui;

/// <summary>
/// Pure Avalonia 3D drafting viewport (no Raylib): perspective wireframe, box grid,
/// snap-to-grid, grip point edit, and axis-locked moves. Drawing plane is <c>Y = DrawElevation</c>.
/// </summary>
public sealed class CadDraft3DViewport : Control
{
    private readonly CadDocumentSession _session;
    private readonly CadEditorSettings _settings;
    private readonly CadCommandDispatcher _dispatcher;
    private readonly CadCommandBus _bus;
    private readonly CadToolController _tools;

    private readonly OrbitCameraRig _orbit = new()
    {
        Target = new Vector3(0f, 0.5f, 0f),
        Distance = 22f,
        MinDistance = 1.5f,
        MaxDistance = 500f,
        MinPitch = -1.45f,
        Yaw = 0.85f,
        Pitch = 0.48f,
        FieldOfViewDegrees = 50f,
        SmoothRate = 0f,
    };

    private bool _orbiting;
    private bool _panning;
    private bool _moving;
    private Point _lastPointer;
    private Point _movePointerStart;
    private Vector3 _moveStartHit;
    private Guid? _moveEntityId;
    private EntityGeometrySnapshot? _moveBefore;
    private GripKind? _activeGrip;
    private CadEntity? _gripEntity;

    private const float PickPixels = 14f;

    private static readonly IBrush CanvasBrush = new SolidColorBrush(Color.FromRgb(18, 22, 28));
    private static readonly IPen GridFloorPen = new Pen(new SolidColorBrush(Color.FromArgb(70, 120, 150, 170)), 1);
    private static readonly IPen GridMajorPen = new Pen(new SolidColorBrush(Color.FromArgb(110, 140, 170, 190)), 1.2);
    private static readonly IPen GridVertPen = new Pen(new SolidColorBrush(Color.FromArgb(45, 100, 130, 150)), 1);
    private static readonly IPen SketchPen = new Pen(new SolidColorBrush(Color.FromRgb(190, 210, 230)), 1.4);
    private static readonly IPen SolidPen = new Pen(new SolidColorBrush(Color.FromRgb(140, 170, 190)), 1.5);
    private static readonly IPen SelectedPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 200, 90)), 2.2);
    private static readonly IPen LevelPen = new Pen(
        new SolidColorBrush(Color.FromArgb(160, 255, 180, 70)),
        1.5,
        dashStyle: new DashStyle([4.0, 3.0], 0));
    private static readonly IBrush GripFill = new SolidColorBrush(Color.FromRgb(32, 40, 48));
    private static readonly IPen GripPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 210, 90)), 1.6);
    private static readonly IBrush HudBrush = new SolidColorBrush(Color.FromRgb(210, 220, 230));

    public CadDraft3DViewport(
        CadDocumentSession session,
        CadEditorSettings settings,
        CadCommandDispatcher dispatcher,
        CadCommandBus bus,
        CadToolController tools)
    {
        _session = session;
        _settings = settings;
        _dispatcher = dispatcher;
        _bus = bus;
        _tools = tools;
        Focusable = true;
        ClipToBounds = true;

        _session.Changed += () => InvalidateVisual();
        _tools.Changed += () => InvalidateVisual();
        _dispatcher.ToolChanged += () => InvalidateVisual();
        _dispatcher.ElevationChanged += () => InvalidateVisual();
    }

    public OrbitCameraRig Orbit => _orbit;

    public event Action? ViewChanged;

    public void Fit()
    {
        var (center, radius) = EntityBounds.Compute(_session.Document);
        _orbit.SnapTarget(center + new Vector3(0, MathF.Max(0.4f, radius * 0.12f), 0));
        _orbit.Distance = System.Math.Clamp(MathF.Max(6f, radius * 2.6f), _orbit.MinDistance, _orbit.MaxDistance);
        InvalidateVisual();
        ViewChanged?.Invoke();
    }

    public void OrbitDrag(float dx, float dy)
    {
        _orbit.AddLookDelta(dx * 0.008f, -dy * 0.008f);
        InvalidateVisual();
        ViewChanged?.Invoke();
    }

    public void Pan(float dx, float dy)
    {
        var eye = _orbit.BuildEyePosition();
        var forward = Vector3.Normalize(_orbit.Target - eye);
        var right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        var up = Vector3.Normalize(Vector3.Cross(right, forward));
        var scale = _orbit.Distance * 0.0025f;
        _orbit.Target -= right * dx * scale + up * dy * scale;
        _orbit.SnapTarget(_orbit.Target);
        InvalidateVisual();
        ViewChanged?.Invoke();
    }

    public void Zoom(float wheelDelta)
    {
        _orbit.AdjustDistance(-wheelDelta * MathF.Max(0.4f, _orbit.Distance * 0.08f));
        InvalidateVisual();
        ViewChanged?.Invoke();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        var p = e.GetPosition(this);
        var props = e.GetCurrentPoint(this).Properties;
        var alt = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        if (props.IsMiddleButtonPressed || props.IsRightButtonPressed || (props.IsLeftButtonPressed && alt))
        {
            if (shift && props.IsMiddleButtonPressed)
                _panning = true;
            else
                _orbiting = true;
            _lastPointer = p;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (!props.IsLeftButtonPressed)
            return;

        var eye = _orbit.BuildEyePosition();
        var vp = BuildViewProjection(eye, _orbit.Target, Bounds.Size);

        if (_dispatcher.ActiveTool == CadToolKind.Select)
        {
            // Screen-space pick — never snap before hit-test (snap pulls the ray off the entity).
            if (TryBeginGrip(p, vp))
            {
                e.Pointer.Capture(this);
                InvalidateVisual();
                e.Handled = true;
                return;
            }

            var entity = HitTestScreen(p, vp);
            if (entity is not null)
            {
                _session.SelectedId = entity.Id;
                _session.Notify();
                _moving = true;
                _moveEntityId = entity.Id;
                _movePointerStart = p;
                _lastPointer = p;
                _moveBefore = EntityGeometrySnapshot.Capture(entity);
                TryHitElevation(p, out _moveStartHit);
                e.Pointer.Capture(this);
            }
            else
            {
                _session.SelectedId = null;
                _session.Notify();
            }

            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (!TryHitElevation(p, out var hit))
        {
            e.Handled = true;
            return;
        }

        _tools.OnClick(SnapPlanar(hit), pixelsPerMeter: EstimatePixelsPerMeterAt(hit));
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var p = e.GetPosition(this);
        if (_orbiting)
        {
            OrbitDrag((float)(p.X - _lastPointer.X), (float)(p.Y - _lastPointer.Y));
            _lastPointer = p;
            e.Handled = true;
            return;
        }

        if (_panning)
        {
            Pan((float)(p.X - _lastPointer.X), (float)(p.Y - _lastPointer.Y));
            _lastPointer = p;
            e.Handled = true;
            return;
        }

        if (_activeGrip is not null && _gripEntity is not null && _moveBefore is not null)
        {
            ApplyGripDrag(p);
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_moving && _moveEntityId is { } id && _moveBefore is not null)
        {
            var entity = _session.Document.Entities.FirstOrDefault(x => x.Id == id);
            if (entity is not null)
            {
                _moveBefore.ApplyTo(entity);
                var delta = ComputeMoveDelta(p);
                CadVec.TranslateEntity(entity, delta.X, delta.Y, delta.Z);
                _session.Notify();
            }

            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_dispatcher.ActiveTool != CadToolKind.Select && TryHitElevation(p, out var hover))
        {
            _tools.OnHover(SnapPlanar(hover));
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if ((_moving || _activeGrip is not null) && _moveBefore is not null)
        {
            var id = _moveEntityId ?? _gripEntity?.Id;
            var entity = id is { } eid
                ? _session.Document.Entities.FirstOrDefault(x => x.Id == eid)
                : null;
            if (entity is not null)
            {
                var after = EntityGeometrySnapshot.Capture(entity);
                _moveBefore.ApplyTo(entity);
                _bus.Execute(new MutateEntityGeometryCommand(entity.Id, _moveBefore, after));
            }
        }

        _orbiting = false;
        _panning = false;
        _moving = false;
        _moveEntityId = null;
        _moveBefore = null;
        _activeGrip = null;
        _gripEntity = null;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        Zoom((float)e.Delta.Y);
        e.Handled = true;
    }

    public override void Render(DrawingContext context)
    {
        var size = Bounds.Size;
        if (size.Width < 2 || size.Height < 2)
            return;

        context.FillRectangle(CanvasBrush, new Rect(size));

        var eye = _orbit.BuildEyePosition();
        var target = _orbit.Target;
        var vp = BuildViewProjection(eye, target, size);
        var elev = _settings.Settings.DrawElevation;

        DrawBoxGrid(context, vp, size, elev);
        DrawLevelPlane(context, vp, size, elev);

        foreach (var entity in _session.Document.Entities)
        {
            if (_settings.Settings.IsolateLevel
                && !CadVec.MatchesLevel(entity, elev, _settings.Settings.LevelTolerance))
                continue;

            var selected = _session.SelectedId == entity.Id;
            var pen = selected ? SelectedPen : IsSolid(entity) ? SolidPen : SketchPen;
            DrawEntity(context, vp, size, entity, pen);
        }

        DrawGrips(context, vp, size);
        DrawHud(context, size);
    }

    private void DrawBoxGrid(DrawingContext context, Matrix4x4 vp, Size size, float elev)
    {
        var step = MathF.Max(0.05f, _settings.Settings.GridStep);
        var extent = MathF.Max(8f, MathF.Ceiling(_orbit.Distance * 0.85f / step) * step);
        extent = System.Math.Min(extent, 40f);
        var y0 = elev;
        var y1 = elev + MathF.Max(step * 4f, MathF.Min(12f, extent * 0.45f));

        for (var x = -extent; x <= extent + 1e-4f; x += step)
        {
            var major = MathF.Abs(x % (step * 5f)) < step * 0.01f || MathF.Abs(x) < 1e-4f;
            DrawWorldLine(context, vp, size, new Vector3(x, y0, -extent), new Vector3(x, y0, extent),
                major ? GridMajorPen : GridFloorPen);
        }

        for (var z = -extent; z <= extent + 1e-4f; z += step)
        {
            var major = MathF.Abs(z % (step * 5f)) < step * 0.01f || MathF.Abs(z) < 1e-4f;
            DrawWorldLine(context, vp, size, new Vector3(-extent, y0, z), new Vector3(extent, y0, z),
                major ? GridMajorPen : GridFloorPen);
        }

        var majorStep = step * 5f;
        for (var x = -extent; x <= extent + 1e-4f; x += majorStep)
        {
            for (var z = -extent; z <= extent + 1e-4f; z += majorStep)
                DrawWorldLine(context, vp, size, new Vector3(x, y0, z), new Vector3(x, y1, z), GridVertPen);
        }

        DrawWorldLine(context, vp, size, new Vector3(-extent, y1, -extent), new Vector3(extent, y1, -extent), GridVertPen);
        DrawWorldLine(context, vp, size, new Vector3(extent, y1, -extent), new Vector3(extent, y1, extent), GridVertPen);
        DrawWorldLine(context, vp, size, new Vector3(extent, y1, extent), new Vector3(-extent, y1, extent), GridVertPen);
        DrawWorldLine(context, vp, size, new Vector3(-extent, y1, extent), new Vector3(-extent, y1, -extent), GridVertPen);
    }

    private void DrawLevelPlane(DrawingContext context, Matrix4x4 vp, Size size, float elev)
    {
        var e = MathF.Max(2f, _settings.Settings.GridStep * 4f);
        DrawWorldLine(context, vp, size, new Vector3(-e, elev, -e), new Vector3(e, elev, -e), LevelPen);
        DrawWorldLine(context, vp, size, new Vector3(e, elev, -e), new Vector3(e, elev, e), LevelPen);
        DrawWorldLine(context, vp, size, new Vector3(e, elev, e), new Vector3(-e, elev, e), LevelPen);
        DrawWorldLine(context, vp, size, new Vector3(-e, elev, e), new Vector3(-e, elev, -e), LevelPen);
    }

    private void DrawGrips(DrawingContext context, Matrix4x4 vp, Size size)
    {
        if (_dispatcher.ActiveTool != CadToolKind.Select)
            return;
        var selected = _session.SelectedEntity;
        if (selected is null)
            return;

        foreach (var (_, world) in EnumerateGrips(selected))
        {
            if (!TryProject(vp, size, world, out var s))
                continue;
            context.DrawRectangle(GripFill, GripPen, new Rect(s.X - 5, s.Y - 5, 10, 10));
        }
    }

    private void DrawEntity(DrawingContext context, Matrix4x4 vp, Size size, CadEntity entity, IPen pen)
    {
        switch (entity.Kind.ToLowerInvariant())
        {
            case "line" or "wall" or "dimension" when entity.A is not null && entity.B is not null:
            {
                var a = CadVec.To(entity.A);
                var b = CadVec.To(entity.B);
                DrawWorldLine(context, vp, size, a, b, pen);
                if (entity.Kind.Equals("wall", StringComparison.OrdinalIgnoreCase))
                {
                    var h = entity.Height > 0 ? entity.Height : 2.4f;
                    DrawWorldLine(context, vp, size, a + new Vector3(0, h, 0), b + new Vector3(0, h, 0), pen);
                    DrawWorldLine(context, vp, size, a, a + new Vector3(0, h, 0), pen);
                    DrawWorldLine(context, vp, size, b, b + new Vector3(0, h, 0), pen);
                }

                break;
            }
            case "rect" when entity.A is not null && entity.B is not null:
            {
                var a = CadVec.To(entity.A);
                var b = CadVec.To(entity.B);
                var p0 = new Vector3(a.X, a.Y, a.Z);
                var p1 = new Vector3(b.X, a.Y, a.Z);
                var p2 = new Vector3(b.X, a.Y, b.Z);
                var p3 = new Vector3(a.X, a.Y, b.Z);
                DrawWorldLine(context, vp, size, p0, p1, pen);
                DrawWorldLine(context, vp, size, p1, p2, pen);
                DrawWorldLine(context, vp, size, p2, p3, pen);
                DrawWorldLine(context, vp, size, p3, p0, pen);
                break;
            }
            case "circle" when entity.Center is not null:
            {
                var c = CadVec.To(entity.Center);
                var r = entity.Radius;
                Vector3? prev = null;
                for (var i = 0; i <= 32; i++)
                {
                    var ang = i / 32f * MathF.PI * 2f;
                    var pt = c + new Vector3(MathF.Cos(ang) * r, 0, MathF.Sin(ang) * r);
                    if (prev is { } prevPt)
                        DrawWorldLine(context, vp, size, prevPt, pt, pen);
                    prev = pt;
                }

                break;
            }
            case "box" when entity.Center is not null && entity.HalfExtents is { Length: >= 3 }:
            {
                var c = CadVec.To(entity.Center);
                var hx = entity.HalfExtents[0];
                var hy = entity.HalfExtents[1];
                var hz = entity.HalfExtents[2];
                var corners = new Vector3[8];
                var n = 0;
                for (var iy = -1; iy <= 1; iy += 2)
                for (var iz = -1; iz <= 1; iz += 2)
                for (var ix = -1; ix <= 1; ix += 2)
                    corners[n++] = c + new Vector3(ix * hx, iy * hy, iz * hz);

                int[] edges =
                [
                    0, 1, 1, 3, 3, 2, 2, 0,
                    4, 5, 5, 7, 7, 6, 6, 4,
                    0, 4, 1, 5, 2, 6, 3, 7,
                ];
                for (var i = 0; i < edges.Length; i += 2)
                    DrawWorldLine(context, vp, size, corners[edges[i]], corners[edges[i + 1]], pen);
                break;
            }
            case "sphere" when entity.Center is not null:
            {
                var c = CadVec.To(entity.Center);
                var r = entity.Radius;
                DrawCircleXZ(context, vp, size, c, r, pen);
                DrawCircleXY(context, vp, size, c, r, pen);
                DrawCircleYZ(context, vp, size, c, r, pen);
                break;
            }
            case "cylinder" when entity.Center is not null:
            {
                var c = CadVec.To(entity.Center);
                var r = entity.Radius;
                var h = entity.Height > 0 ? entity.Height : r * 2;
                var bottom = c - new Vector3(0, h * 0.5f, 0);
                var top = c + new Vector3(0, h * 0.5f, 0);
                DrawCircleXZ(context, vp, size, bottom, r, pen);
                DrawCircleXZ(context, vp, size, top, r, pen);
                for (var i = 0; i < 4; i++)
                {
                    var ang = i / 4f * MathF.PI * 2f;
                    var o = new Vector3(MathF.Cos(ang) * r, 0, MathF.Sin(ang) * r);
                    DrawWorldLine(context, vp, size, bottom + o, top + o, pen);
                }

                break;
            }
            default:
            {
                var pts = CadVec.EnumerateWorldPoints(entity).ToList();
                for (var i = 1; i < pts.Count; i++)
                    DrawWorldLine(context, vp, size, pts[i - 1], pts[i], pen);
                break;
            }
        }
    }

    private static void DrawCircleXZ(DrawingContext context, Matrix4x4 vp, Size size, Vector3 c, float r, IPen pen)
    {
        Vector3? prev = null;
        for (var i = 0; i <= 24; i++)
        {
            var ang = i / 24f * MathF.PI * 2f;
            var pt = c + new Vector3(MathF.Cos(ang) * r, 0, MathF.Sin(ang) * r);
            if (prev is { } p)
                DrawWorldLine(context, vp, size, p, pt, pen);
            prev = pt;
        }
    }

    private static void DrawCircleXY(DrawingContext context, Matrix4x4 vp, Size size, Vector3 c, float r, IPen pen)
    {
        Vector3? prev = null;
        for (var i = 0; i <= 24; i++)
        {
            var ang = i / 24f * MathF.PI * 2f;
            var pt = c + new Vector3(MathF.Cos(ang) * r, MathF.Sin(ang) * r, 0);
            if (prev is { } p)
                DrawWorldLine(context, vp, size, p, pt, pen);
            prev = pt;
        }
    }

    private static void DrawCircleYZ(DrawingContext context, Matrix4x4 vp, Size size, Vector3 c, float r, IPen pen)
    {
        Vector3? prev = null;
        for (var i = 0; i <= 24; i++)
        {
            var ang = i / 24f * MathF.PI * 2f;
            var pt = c + new Vector3(0, MathF.Cos(ang) * r, MathF.Sin(ang) * r);
            if (prev is { } p)
                DrawWorldLine(context, vp, size, p, pt, pen);
            prev = pt;
        }
    }

    private void DrawHud(DrawingContext context, Size size)
    {
        var snap = _settings.Settings.SnapToGrid ? "SNAP" : "snap off";
        var lockAxis = _settings.Settings.AxisLock.ToUpperInvariant();
        if (lockAxis is "NONE" or "")
            lockAxis = "FREE";
        else
            lockAxis = "LOCK " + lockAxis;
        var text =
            $"Draft 3D  ·  {snap}  ·  grid {_settings.Settings.GridStep:0.##} m  ·  {lockAxis}  ·  level {_settings.Settings.DrawElevation:0.##} m  ·  click grips · MMB orbit · drag = move";
        var typeface = new Typeface("Segoe UI, Consolas, monospace");
        var formatted = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            typeface,
            12,
            HudBrush);
        context.DrawText(formatted, new Point(10, size.Height - 22));
    }

    private static void DrawWorldLine(
        DrawingContext context,
        Matrix4x4 vp,
        Size size,
        Vector3 a,
        Vector3 b,
        IPen pen)
    {
        if (!TryProject(vp, size, a, out var sa) || !TryProject(vp, size, b, out var sb))
            return;
        context.DrawLine(pen, sa, sb);
    }

    private Matrix4x4 BuildViewProjection(Vector3 eye, Vector3 target, Size size)
    {
        var view = Matrix4x4.CreateLookAt(eye, target, Vector3.UnitY);
        var aspect = (float)(size.Width / System.Math.Max(1, size.Height));
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(
            _orbit.FieldOfViewDegrees * (MathF.PI / 180f),
            aspect,
            0.05f,
            2000f);
        return view * proj;
    }

    private static bool TryProject(Matrix4x4 vp, Size size, Vector3 world, out Point screen)
    {
        screen = default;
        var clip = Vector4.Transform(new Vector4(world, 1f), vp);
        if (clip.W <= 1e-5f)
            return false;
        var ndc = new Vector3(clip.X, clip.Y, clip.Z) / clip.W;
        if (ndc.Z < -1.05f || ndc.Z > 1.05f)
            return false;
        var x = (ndc.X * 0.5f + 0.5f) * size.Width;
        var y = (1f - (ndc.Y * 0.5f + 0.5f)) * size.Height;
        screen = new Point(x, y);
        return true;
    }

    private bool TryHitElevation(Point screen, out Vector3 hit) =>
        CadModelPick.TryHitElevationPlane(
            _orbit.BuildEyePosition(),
            _orbit.Target,
            _orbit.FieldOfViewDegrees,
            Bounds.Size,
            screen,
            _settings.Settings.DrawElevation,
            out hit);

    /// <summary>
    /// Axis-constrained move delta. Y lock maps screen pixels → meters (stable), not ray/plane (explosive).
    /// </summary>
    private Vector3 ComputeMoveDelta(Point screen)
    {
        var ppm = MathF.Max(8f, EstimatePixelsPerMeterAt(_moveStartHit));
        var lockAxis = _settings.Settings.AxisLock.Trim().ToLowerInvariant();

        if (lockAxis == "y")
        {
            var dyPixels = (float)(screen.Y - _movePointerStart.Y);
            var worldDy = -dyPixels / ppm; // screen up → world up
            if (_settings.Settings.SnapToGrid)
            {
                var step = MathF.Max(0.001f, _settings.Settings.GridStep);
                worldDy = MathF.Round(worldDy / step) * step;
            }

            return new Vector3(0, worldDy, 0);
        }

        if (!TryHitElevation(screen, out var hit))
            return Vector3.Zero;

        hit = SnapPlanar(hit);
        var delta = hit - _moveStartHit;
        return lockAxis switch
        {
            "x" => new Vector3(delta.X, 0, 0),
            "z" => new Vector3(0, 0, delta.Z),
            _ => delta with { Y = 0 },
        };
    }

    private Vector3 SnapPlanar(Vector3 world)
    {
        var y = _settings.Settings.DrawElevation;
        if (!_settings.Settings.SnapToGrid)
            return world with { Y = y };
        var step = MathF.Max(0.001f, _settings.Settings.GridStep);
        return new Vector3(
            MathF.Round(world.X / step) * step,
            y,
            MathF.Round(world.Z / step) * step);
    }

    private bool TryBeginGrip(Point screen, Matrix4x4 vp)
    {
        var selected = _session.SelectedEntity;
        if (selected is null)
            return false;

        var size = Bounds.Size;
        GripKind? best = null;
        var bestDist = PickPixels;
        foreach (var (kind, world) in EnumerateGrips(selected))
        {
            if (!TryProject(vp, size, world, out var s))
                continue;
            var d = (float)Point.Distance(screen, s);
            if (d <= bestDist)
            {
                bestDist = d;
                best = kind;
            }
        }

        if (best is null)
            return false;

        _activeGrip = best;
        _gripEntity = selected;
        _moveBefore = EntityGeometrySnapshot.Capture(selected);
        _movePointerStart = screen;
        _lastPointer = screen;
        TryHitElevation(screen, out _moveStartHit);
        return true;
    }

    private void ApplyGripDrag(Point screen)
    {
        if (_gripEntity is null || _activeGrip is null || _moveBefore is null)
            return;

        _moveBefore.ApplyTo(_gripEntity);
        var lockAxis = _settings.Settings.AxisLock.Trim().ToLowerInvariant();
        Vector3 world;
        if (lockAxis == "y")
        {
            var ppm = MathF.Max(8f, EstimatePixelsPerMeterAt(_moveStartHit));
            var dyPixels = (float)(screen.Y - _movePointerStart.Y);
            var worldDy = -dyPixels / ppm;
            if (_settings.Settings.SnapToGrid)
            {
                var step = MathF.Max(0.001f, _settings.Settings.GridStep);
                worldDy = MathF.Round(worldDy / step) * step;
            }

            world = _moveStartHit + new Vector3(0, worldDy, 0);
        }
        else
        {
            if (!TryHitElevation(screen, out world))
                return;
            world = SnapPlanar(world);
            if (lockAxis == "x")
                world = new Vector3(world.X, _moveStartHit.Y, _moveStartHit.Z);
            else if (lockAxis == "z")
                world = new Vector3(_moveStartHit.X, _moveStartHit.Y, world.Z);
        }

        ApplyGripToEntity(_gripEntity, _activeGrip.Value, world);
        _session.Notify();
    }

    private static void ApplyGripToEntity(CadEntity entity, GripKind grip, Vector3 world)
    {
        switch (entity.Kind.ToLowerInvariant())
        {
            case "line" or "wall" or "dimension":
                if (grip == GripKind.LineA)
                    entity.A = CadVec.Xyz(world.X, world.Y, world.Z);
                else if (grip == GripKind.LineB)
                    entity.B = CadVec.Xyz(world.X, world.Y, world.Z);
                break;

            case "rect" when entity.A is not null && entity.B is not null:
            {
                var a = CadVec.To(entity.A);
                if (grip == GripKind.RectA)
                    entity.A = CadVec.Xyz(world.X, a.Y, world.Z);
                else if (grip == GripKind.RectB)
                    entity.B = CadVec.Xyz(world.X, a.Y, world.Z);
                break;
            }

            case "circle" when entity.Center is not null && grip == GripKind.CircleRadius:
            {
                var c = CadVec.To(entity.Center);
                entity.Radius = MathF.Max(0.05f, Vector3.Distance(
                    new Vector3(world.X, 0, world.Z),
                    new Vector3(c.X, 0, c.Z)));
                break;
            }

            case "circle" when entity.Center is not null && grip == GripKind.CircleCenter:
                entity.Center = CadVec.Xyz(world.X, world.Y, world.Z);
                break;

            case "box" when entity.Center is not null && entity.HalfExtents is { Length: >= 3 }:
            {
                var c = CadVec.To(entity.Center);
                var he = entity.HalfExtents;
                switch (grip)
                {
                    case GripKind.BoxMinX:
                    {
                        var maxX = c.X + he[0];
                        var newHx = MathF.Max(0.05f, (maxX - world.X) * 0.5f);
                        he[0] = newHx;
                        entity.Center = CadVec.Xyz(maxX - newHx, c.Y, c.Z);
                        break;
                    }
                    case GripKind.BoxMaxX:
                    {
                        var minX = c.X - he[0];
                        var newHx = MathF.Max(0.05f, (world.X - minX) * 0.5f);
                        he[0] = newHx;
                        entity.Center = CadVec.Xyz(minX + newHx, c.Y, c.Z);
                        break;
                    }
                    case GripKind.BoxMinZ:
                    {
                        var maxZ = c.Z + he[2];
                        var newHz = MathF.Max(0.05f, (maxZ - world.Z) * 0.5f);
                        he[2] = newHz;
                        entity.Center = CadVec.Xyz(c.X, c.Y, maxZ - newHz);
                        break;
                    }
                    case GripKind.BoxMaxZ:
                    {
                        var minZ = c.Z - he[2];
                        var newHz = MathF.Max(0.05f, (world.Z - minZ) * 0.5f);
                        he[2] = newHz;
                        entity.Center = CadVec.Xyz(c.X, c.Y, minZ + newHz);
                        break;
                    }
                    case GripKind.BoxCenter:
                        entity.Center = CadVec.Xyz(world.X, world.Y, world.Z);
                        break;
                }

                break;
            }
        }
    }

    private CadEntity? HitTestScreen(Point screen, Matrix4x4 vp)
    {
        var size = Bounds.Size;
        CadEntity? best = null;
        var bestDist = PickPixels;
        foreach (var entity in _session.Document.Entities)
        {
            if (_settings.Settings.IsolateLevel
                && !CadVec.MatchesLevel(entity, _settings.Settings.DrawElevation, _settings.Settings.LevelTolerance))
                continue;

            var d = ScreenDistanceToEntity(entity, screen, vp, size);
            if (d < bestDist)
            {
                bestDist = d;
                best = entity;
            }
        }

        return best;
    }

    private static float ScreenDistanceToEntity(CadEntity entity, Point screen, Matrix4x4 vp, Size size)
    {
        var best = float.MaxValue;
        switch (entity.Kind.ToLowerInvariant())
        {
            case "line" or "wall" or "dimension" when entity.A is not null && entity.B is not null:
                best = System.Math.Min(best, ScreenDistSegment(screen, vp, size, CadVec.To(entity.A), CadVec.To(entity.B)));
                break;
            case "rect" when entity.A is not null && entity.B is not null:
            {
                var a = CadVec.To(entity.A);
                var b = CadVec.To(entity.B);
                var p0 = new Vector3(a.X, a.Y, a.Z);
                var p1 = new Vector3(b.X, a.Y, a.Z);
                var p2 = new Vector3(b.X, a.Y, b.Z);
                var p3 = new Vector3(a.X, a.Y, b.Z);
                best = System.Math.Min(best, ScreenDistSegment(screen, vp, size, p0, p1));
                best = System.Math.Min(best, ScreenDistSegment(screen, vp, size, p1, p2));
                best = System.Math.Min(best, ScreenDistSegment(screen, vp, size, p2, p3));
                best = System.Math.Min(best, ScreenDistSegment(screen, vp, size, p3, p0));
                break;
            }
            case "circle" when entity.Center is not null:
            {
                var c = CadVec.To(entity.Center);
                if (TryProject(vp, size, c, out var cs))
                    best = System.Math.Min(best, (float)Point.Distance(screen, cs));
                for (var i = 0; i < 16; i++)
                {
                    var ang = i / 16f * MathF.PI * 2f;
                    var pt = c + new Vector3(MathF.Cos(ang) * entity.Radius, 0, MathF.Sin(ang) * entity.Radius);
                    if (TryProject(vp, size, pt, out var s))
                        best = System.Math.Min(best, (float)Point.Distance(screen, s));
                }

                break;
            }
            default:
                foreach (var pt in CadVec.EnumerateWorldPoints(entity))
                {
                    if (TryProject(vp, size, pt, out var s))
                        best = System.Math.Min(best, (float)Point.Distance(screen, s));
                }

                break;
        }

        return best;
    }

    private static float ScreenDistSegment(Point screen, Matrix4x4 vp, Size size, Vector3 a, Vector3 b)
    {
        if (!TryProject(vp, size, a, out var sa) || !TryProject(vp, size, b, out var sb))
            return float.MaxValue;
        return DistPointSeg2D(screen, sa, sb);
    }

    private static float DistPointSeg2D(Point p, Point a, Point b)
    {
        var abx = b.X - a.X;
        var aby = b.Y - a.Y;
        var len2 = abx * abx + aby * aby;
        if (len2 < 1e-8)
            return (float)Point.Distance(p, a);
        var t = ((p.X - a.X) * abx + (p.Y - a.Y) * aby) / len2;
        t = System.Math.Clamp(t, 0, 1);
        var qx = a.X + abx * t;
        var qy = a.Y + aby * t;
        var dx = p.X - qx;
        var dy = p.Y - qy;
        return (float)MathF.Sqrt((float)(dx * dx + dy * dy));
    }

    private static IEnumerable<(GripKind Kind, Vector3 World)> EnumerateGrips(CadEntity entity)
    {
        switch (entity.Kind.ToLowerInvariant())
        {
            case "line" or "wall" or "dimension" when entity.A is not null && entity.B is not null:
                yield return (GripKind.LineA, CadVec.To(entity.A));
                yield return (GripKind.LineB, CadVec.To(entity.B));
                break;
            case "rect" when entity.A is not null && entity.B is not null:
                yield return (GripKind.RectA, CadVec.To(entity.A));
                yield return (GripKind.RectB, CadVec.To(entity.B));
                break;
            case "circle" when entity.Center is not null:
            {
                var c = CadVec.To(entity.Center);
                yield return (GripKind.CircleCenter, c);
                yield return (GripKind.CircleRadius, c + new Vector3(entity.Radius, 0, 0));
                break;
            }
            case "box" when entity.Center is not null && entity.HalfExtents is { Length: >= 3 }:
            {
                var c = CadVec.To(entity.Center);
                var hx = entity.HalfExtents[0];
                var hz = entity.HalfExtents[2];
                yield return (GripKind.BoxCenter, c);
                yield return (GripKind.BoxMinX, new Vector3(c.X - hx, c.Y, c.Z));
                yield return (GripKind.BoxMaxX, new Vector3(c.X + hx, c.Y, c.Z));
                yield return (GripKind.BoxMinZ, new Vector3(c.X, c.Y, c.Z - hz));
                yield return (GripKind.BoxMaxZ, new Vector3(c.X, c.Y, c.Z + hz));
                break;
            }
        }
    }

    private float EstimatePixelsPerMeterAt(Vector3 world)
    {
        var size = Bounds.Size;
        if (size.Width < 1)
            return 40f;
        var eye = _orbit.BuildEyePosition();
        var vp = BuildViewProjection(eye, _orbit.Target, size);
        if (!TryProject(vp, size, world, out var a)
            || !TryProject(vp, size, world + new Vector3(1, 0, 0), out var b))
            return MathF.Max(12f, 900f / MathF.Max(1f, _orbit.Distance));
        var ppm = (float)Point.Distance(a, b);
        return ppm > 1f ? ppm : MathF.Max(12f, 900f / MathF.Max(1f, _orbit.Distance));
    }

    private float EstimatePixelsPerMeter() =>
        EstimatePixelsPerMeterAt(new Vector3(0, _settings.Settings.DrawElevation, 0));

    private static bool IsSolid(CadEntity e) =>
        e.Kind.Equals("box", StringComparison.OrdinalIgnoreCase)
        || e.Kind.Equals("sphere", StringComparison.OrdinalIgnoreCase)
        || e.Kind.Equals("cylinder", StringComparison.OrdinalIgnoreCase)
        || e.Kind.Equals("wall", StringComparison.OrdinalIgnoreCase);

    private enum GripKind
    {
        LineA,
        LineB,
        RectA,
        RectB,
        CircleCenter,
        CircleRadius,
        BoxCenter,
        BoxMinX,
        BoxMaxX,
        BoxMinZ,
        BoxMaxZ,
    }
}
