using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Novolis.Agent.Core;
using Novolis.Agent.Surface;
using Novolis.Avalonia.Raylib;
using Novolis.Avalonia._3D.Services;
using Novolis.Avalonia._3D.Session;
using Novolis._3D;

namespace Novolis.Avalonia._3D.Ui;

/// <summary>CAD viewport host. Default presenter: <see cref="SceneViewportBackendKind.OpenGl"/>.</summary>
public sealed class SceneViewportControl : Panel
{
    private readonly SceneSessionService _session;
    private readonly SceneViewportCamera _camera;
    private readonly SceneViewportBackendKind _backend;
    private readonly SceneViewportRenderer? _raylibRenderer;
    private readonly RaylibHostControl? _raylibHost;
    private readonly SceneWireGlControl? _gl;
    private readonly SceneWireCpuControl? _cpu;
    private readonly SceneWireVulkanControl? _vulkan;
    private Point? _last;
    private bool _orbiting;
    private bool _panning;
    private bool _draggingGizmo;
    private bool _potentialPick;

    public SceneViewportControl(
        SceneSessionService session,
        SceneViewportBackendKind backend = SceneViewportBackendKind.OpenGl,
        SceneViewportCamera? sharedCamera = null)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _backend = backend;
        _camera = sharedCamera ?? new SceneViewportCamera(session);
        FrameMeter = new ViewportFrameMeter();
        Background = new SolidColorBrush(Color.FromRgb(18, 24, 32));
        Focusable = true;
        ClipToBounds = true;

        switch (backend)
        {
            case SceneViewportBackendKind.Cpu:
                _cpu = new SceneWireCpuControl(session, _camera, FrameMeter) { IsHitTestVisible = false };
                Children.Add(_cpu);
                break;
            case SceneViewportBackendKind.Raylib:
                _raylibHost = new RaylibHostControl { IsHitTestVisible = false };
                _raylibRenderer = new SceneViewportRenderer(session, _camera) { FrameMeter = FrameMeter };
                Children.Add(_raylibHost);
                _raylibRenderer.Bind(_raylibHost);
                break;
            case SceneViewportBackendKind.Vulkan:
                _vulkan = new SceneWireVulkanControl(session, _camera, FrameMeter) { IsHitTestVisible = false };
                Children.Add(_vulkan);
                break;
            default:
                // Presenter paints; this host owns all mouse input (otherwise OpenGL eats hits).
                _gl = new SceneWireGlControl(session, _camera, FrameMeter) { IsHitTestVisible = false };
                Children.Add(_gl);
                break;
        }

        LayoutUpdated += (_, _) =>
        {
            if (_raylibHost is not null)
                SyncRaylibResolution();
        };
        _session.DocumentChanged += () =>
        {
            RefreshGizmoOrigin();
            RequestPresent();
        };
        PointerPressed += OnPressed;
        PointerMoved += OnMoved;
        PointerReleased += OnReleased;
        PointerWheelChanged += (_, e) =>
        {
            _camera.Zoom((float)e.Delta.Y);
            e.Handled = true;
        };
        RefreshGizmoOrigin();
    }

    public SceneViewportBackendKind Backend => _backend;
    public SceneViewportCamera Camera => _camera;
    public ViewportFrameMeter FrameMeter { get; }
    public RaylibHostControl? Host => _raylibHost;
    public SceneViewportRenderer? RaylibRenderer => _raylibRenderer;

    /// <summary>OpenGL or Vulkan init/present error, when available.</summary>
    public string? LastError => _gl?.LastError ?? _vulkan?.LastError;

    /// <summary>Legacy accessor used by Raylib present loop.</summary>
    public SceneViewportRenderer Renderer =>
        _raylibRenderer ?? throw new InvalidOperationException("Raylib renderer only available for Raylib backend.");

    public void Start()
    {
        if (_raylibHost is not null)
        {
            SyncRaylibResolution();
            _raylibHost.SetHostActive(true);
            _raylibHost.EnsureHostStarted();
        }

        _cpu?.Start();
        _vulkan?.Start();
    }

    public void Stop()
    {
        _raylibHost?.SetHostActive(false);
        _cpu?.Stop();
        _vulkan?.Stop();
    }

    public void Fit() => _camera.Fit();

    public void RequestPresent()
    {
        _raylibHost?.RequestFrame();
        _gl?.RequestPresent();
        _cpu?.InvalidateVisual();
        _vulkan?.RequestPresent();
        InvalidateVisual();
    }

    /// <summary>Captures viewport PNG (OpenGL readback when available; otherwise Avalonia render).</summary>
    public Task<bool> CapturePngAsync(string path)
    {
        if (_gl is not null)
            return _gl.CapturePngAsync(path);
        return Task.FromResult(SceneViewportExporter.TryExportControlPng(this, path));
    }

    private void SyncRaylibResolution()
    {
        if (_raylibHost is null)
            return;
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w < 8 || h < 8)
            return;
        var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        var ss = System.Math.Clamp(scale * 1.5, 1.0, 2.5);
        var fw = (int)System.Math.Clamp(System.Math.Round(w * ss), 64, 8192);
        var fh = (int)System.Math.Clamp(System.Math.Round(h * ss), 64, 8192);
        if (_raylibHost.FrameWidth == fw && _raylibHost.FrameHeight == fh)
            return;
        _raylibHost.FrameWidth = fw;
        _raylibHost.FrameHeight = fh;
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        Focus();
        var pt = e.GetCurrentPoint(this);
        var mods = e.KeyModifiers;
        _last = e.GetPosition(this);

        // Pan: Shift+MMB or Shift+Alt+LMB
        if ((pt.Properties.IsMiddleButtonPressed && mods.HasFlag(KeyModifiers.Shift))
            || (pt.Properties.IsLeftButtonPressed && mods.HasFlag(KeyModifiers.Alt) && mods.HasFlag(KeyModifiers.Shift)))
        {
            _panning = true;
            _orbiting = false;
            _potentialPick = false;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        // Orbit: middle button, right button, or Alt+left (CAD convention).
        if (pt.Properties.IsMiddleButtonPressed
            || pt.Properties.IsRightButtonPressed
            || (pt.Properties.IsLeftButtonPressed && mods.HasFlag(KeyModifiers.Alt)))
        {
            _orbiting = true;
            _panning = false;
            _potentialPick = false;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (!pt.Properties.IsLeftButtonPressed)
            return;

        RefreshGizmoOrigin();
        _potentialPick = true;
        _draggingGizmo = _camera.GizmoOrigin is not null && NearGizmo(_last.Value);
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

        if (_panning)
        {
            _last = p;
            _camera.Pan(dx, dy);
            return;
        }

        if (_orbiting)
        {
            _last = p;
            _camera.OrbitDrag(dx, dy);
            return;
        }

        if (_draggingGizmo && (MathF.Abs(dx) + MathF.Abs(dy) > 0.5f))
        {
            _potentialPick = false;
            var scale = _camera.Orbit.Distance * 0.0025f;
            _session.Execute(new AgentCommand
            {
                ActionId = SceneSessionActionIds.MoveSelection,
                X = dx * scale,
                Y = -dy * scale,
                Z = 0,
            });
            RefreshGizmoOrigin();
            _last = p;
            return;
        }

        // Do not convert plain left-drag into orbit — that steals selection clicks.
        if (_potentialPick && (MathF.Abs(dx) + MathF.Abs(dy) > 4f) && !_draggingGizmo)
            _potentialPick = false;

        _last = p;
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_potentialPick && !_orbiting && !_panning && !_draggingGizmo && _last is not null)
            ApplyPick(_last.Value, e.KeyModifiers.HasFlag(KeyModifiers.Shift));

        _orbiting = false;
        _panning = false;
        _draggingGizmo = false;
        _potentialPick = false;
        _last = null;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void ApplyPick(Point local, bool additive)
    {
        var hit = _camera.PickAt((float)local.X, (float)local.Y, (float)Bounds.Width, (float)Bounds.Height);
        if (hit is null)
        {
            if (!additive && _session.Document.Edit.Mode != SceneEditMode.Object)
            {
                _session.Execute(new AgentCommand
                {
                    ActionId = SceneSessionActionIds.SelectComponents,
                    Indices = "",
                });
            }

            RefreshGizmoOrigin();
            return;
        }

        var h = hit.Value;
        if (_session.Document.Edit.Mode == SceneEditMode.Object)
        {
            _session.Execute(new AgentCommand
            {
                ActionId = SceneSessionActionIds.Select,
                NodeId = h.SourceId.ToString(),
            });
            RefreshGizmoOrigin();
            return;
        }

        _session.Document.Edit.EditMeshId = h.SourceId;
        _session.Document.SelectionId = h.SourceId;
        var indices = h.Mode == SceneEditMode.Edge
            ? $"{h.Index}-{h.IndexB}"
            : h.Index.ToString();
        _session.Execute(new AgentCommand
        {
            ActionId = SceneSessionActionIds.SelectComponents,
            Indices = indices,
            Additive = additive,
            NodeId = h.SourceId.ToString(),
        });
        RefreshGizmoOrigin();
    }

    private void RefreshGizmoOrigin()
    {
        var edit = _session.Document.Edit;
        Vector3? origin = null;
        if (edit.Mode == SceneEditMode.Object && _session.Document.SelectionId is { } sid)
        {
            var mesh = _session.Evaluator.Cache.EvaluatedMeshes.FirstOrDefault(m => m.SourceId == sid);
            if (mesh is not null && mesh.Vertices.Length > 0)
            {
                var sum = Vector3.Zero;
                foreach (var v in mesh.Vertices)
                    sum += Vector3.Transform(v, mesh.World);
                origin = sum / mesh.Vertices.Length;
            }
            else if (_session.Document.Find(sid) is { } node)
            {
                origin = node.Transform.PositionV;
            }
        }
        else if (edit.Mode != SceneEditMode.Object && edit.EditMeshId is { } mid)
        {
            var mesh = _session.Evaluator.Cache.EvaluatedMeshes.FirstOrDefault(m => m.SourceId == mid);
            if (mesh is not null)
                origin = ComponentCentroid(mesh, edit);
        }

        _camera.GizmoOrigin = origin;
    }

    private static Vector3? ComponentCentroid(EvaluatedMesh mesh, MeshEditState edit)
    {
        var pts = new List<Vector3>();
        if (edit.Mode == SceneEditMode.Point)
        {
            foreach (var i in edit.SelectedVertices)
            {
                if (i >= 0 && i < mesh.Vertices.Length)
                    pts.Add(Vector3.Transform(mesh.Vertices[i], mesh.World));
            }
        }
        else if (edit.Mode == SceneEditMode.Edge)
        {
            foreach (var (a, b) in edit.SelectedEdges)
            {
                if (a >= 0 && a < mesh.Vertices.Length)
                    pts.Add(Vector3.Transform(mesh.Vertices[a], mesh.World));
                if (b >= 0 && b < mesh.Vertices.Length)
                    pts.Add(Vector3.Transform(mesh.Vertices[b], mesh.World));
            }
        }
        else if (edit.Mode == SceneEditMode.Polygon)
        {
            foreach (var f in edit.SelectedFaces)
            {
                if (f < 0 || f * 3 + 2 >= mesh.Indices.Length)
                    continue;
                pts.Add(Vector3.Transform(mesh.Vertices[mesh.Indices[f * 3]], mesh.World));
                pts.Add(Vector3.Transform(mesh.Vertices[mesh.Indices[f * 3 + 1]], mesh.World));
                pts.Add(Vector3.Transform(mesh.Vertices[mesh.Indices[f * 3 + 2]], mesh.World));
            }
        }

        if (pts.Count == 0)
            return null;
        var sum = Vector3.Zero;
        foreach (var p in pts)
            sum += p;
        return sum / pts.Count;
    }

    private bool NearGizmo(Point local)
    {
        if (_camera.GizmoOrigin is not { } origin)
            return false;
        var ray = _camera.BuildScreenRay((float)local.X, (float)local.Y, (float)Bounds.Width, (float)Bounds.Height);
        var w = origin - ray.Position;
        var proj = Vector3.Dot(w, ray.Direction);
        if (proj < 0)
            return false;
        var closest = ray.Position + ray.Direction * proj;
        return Vector3.Distance(closest, origin) < MathF.Max(0.25f, _camera.Orbit.Distance * 0.04f);
    }
}
