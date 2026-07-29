using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Novolis.Agent.Surface;
using Novolis.Avalonia.Raylib;
using Novolis.Avalonia._3D.Services;
using Novolis.Avalonia._3D.Session;
using Novolis.Modeling.Scene;

namespace Novolis.Avalonia._3D.Ui;

public sealed class SceneViewportControl : Panel
{
    private readonly RaylibHostControl _host = new();
    private readonly SceneSessionService _session;
    private readonly SceneViewportRenderer _renderer;
    private Point? _last;
    private bool _orbiting;
    private bool _draggingGizmo;
    private bool _potentialPick;
    private KeyModifiers _mods;

    public SceneViewportControl(SceneSessionService session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _renderer = new SceneViewportRenderer(session);
        Background = new SolidColorBrush(Color.FromRgb(18, 24, 32));
        Children.Add(_host);
        _renderer.Bind(_host);

        PointerPressed += OnPressed;
        PointerMoved += OnMoved;
        PointerReleased += OnReleased;
        PointerWheelChanged += (_, e) =>
        {
            _renderer.Zoom((float)e.Delta.Y);
            e.Handled = true;
        };
    }

    public RaylibHostControl Host => _host;
    public SceneViewportRenderer Renderer => _renderer;

    public void Start()
    {
        _host.SetHostActive(true);
        _host.EnsureHostStarted();
    }

    public void Stop() => _host.SetHostActive(false);

    public void Fit() => _renderer.Fit();

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        var pt = e.GetCurrentPoint(this);
        _mods = e.KeyModifiers;
        _last = e.GetPosition(this);

        if (pt.Properties.IsMiddleButtonPressed || (pt.Properties.IsLeftButtonPressed && _mods.HasFlag(KeyModifiers.Alt)))
        {
            _orbiting = true;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (!pt.Properties.IsLeftButtonPressed)
            return;

        // Start potential pick / gizmo drag; decide on move/release.
        _potentialPick = true;
        _draggingGizmo = _renderer.GizmoOrigin is not null && NearGizmo(_last.Value);
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (_last is null)
            return;
        var p = e.GetPosition(this);
        var dx = (float)(p.X - _last.Value.X);
        var dy = (float)(p.Y - _last.Value.Y);

        if (_orbiting)
        {
            _last = p;
            _renderer.OrbitDrag(dx, dy);
            return;
        }

        if (_draggingGizmo && (MathF.Abs(dx) + MathF.Abs(dy) > 0.5f))
        {
            _potentialPick = false;
            var scale = _renderer.Orbit.Distance * 0.0025f;
            _session.Execute(new AgentCommandDto
            {
                ActionId = SceneSessionActionIds.MoveSelection,
                X = dx * scale,
                Y = -dy * scale,
                Z = 0,
            });
            _last = p;
            return;
        }

        if (_potentialPick && (MathF.Abs(dx) + MathF.Abs(dy) > 4f) && !_draggingGizmo)
        {
            // Drag without Alt = orbit fallback when not on gizmo
            _potentialPick = false;
            _orbiting = true;
            _renderer.OrbitDrag(dx, dy);
            _last = p;
        }
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_potentialPick && !_orbiting && !_draggingGizmo && _last is not null)
            ApplyPick(_last.Value, e.KeyModifiers.HasFlag(KeyModifiers.Shift));

        _orbiting = false;
        _draggingGizmo = false;
        _potentialPick = false;
        _last = null;
        e.Pointer.Capture(null);
    }

    private void ApplyPick(Point local, bool additive)
    {
        var hit = _renderer.PickAt((float)local.X, (float)local.Y, (float)Bounds.Width, (float)Bounds.Height);
        if (hit is null)
        {
            if (!additive && _session.Document.Edit.Mode != SceneEditMode.Object)
            {
                _session.Execute(new AgentCommandDto
                {
                    ActionId = SceneSessionActionIds.SelectComponents,
                    Indices = "",
                });
            }

            return;
        }

        var h = hit.Value;
        if (_session.Document.Edit.Mode == SceneEditMode.Object)
        {
            _session.Execute(new AgentCommandDto
            {
                ActionId = SceneSessionActionIds.Select,
                NodeId = h.SourceId.ToString(),
            });
            return;
        }

        _session.Document.Edit.EditMeshId = h.SourceId;
        _session.Document.SelectionId = h.SourceId;
        var indices = h.Mode == SceneEditMode.Edge
            ? $"{h.Index}-{h.IndexB}"
            : h.Index.ToString();
        _session.Execute(new AgentCommandDto
        {
            ActionId = SceneSessionActionIds.SelectComponents,
            Indices = indices,
            Additive = additive,
            NodeId = h.SourceId.ToString(),
        });
    }

    private bool NearGizmo(Point local)
    {
        if (_renderer.GizmoOrigin is not { } origin)
            return false;
        // Approximate: if pick misses mesh but gizmo drawn, allow drag when close in screen — use pick distance heuristic via world ray closeness
        var ray = _renderer.BuildScreenRay((float)local.X, (float)local.Y, (float)Bounds.Width, (float)Bounds.Height);
        var w = origin - ray.Position;
        var proj = Vector3.Dot(w, ray.Direction);
        if (proj < 0)
            return false;
        var closest = ray.Position + ray.Direction * proj;
        return Vector3.Distance(closest, origin) < MathF.Max(0.25f, _renderer.Orbit.Distance * 0.04f);
    }
}
