using System.Diagnostics;
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
    private bool _draggingGizmo;
    private bool _potentialPick;
    private KeyModifiers _mods;

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

        switch (backend)
        {
            case SceneViewportBackendKind.Cpu:
                _cpu = new SceneWireCpuControl(session, _camera, FrameMeter);
                Children.Add(_cpu);
                break;
            case SceneViewportBackendKind.Raylib:
                _raylibHost = new RaylibHostControl();
                _raylibRenderer = new SceneViewportRenderer(session, _camera) { FrameMeter = FrameMeter };
                Children.Add(_raylibHost);
                _raylibRenderer.Bind(_raylibHost);
                break;
            case SceneViewportBackendKind.Vulkan:
                _vulkan = new SceneWireVulkanControl(session, _camera, FrameMeter);
                Children.Add(_vulkan);
                break;
            default:
                _gl = new SceneWireGlControl(session, _camera, FrameMeter);
                Children.Add(_gl);
                break;
        }

        LayoutUpdated += (_, _) =>
        {
            if (_raylibHost is not null)
                SyncRaylibResolution();
        };
        PointerPressed += OnPressed;
        PointerMoved += OnMoved;
        PointerReleased += OnReleased;
        PointerWheelChanged += (_, e) =>
        {
            _camera.Zoom((float)e.Delta.Y);
            e.Handled = true;
        };
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
            _potentialPick = false;
            _orbiting = true;
            _camera.OrbitDrag(dx, dy);
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
        var hit = _camera.PickAt((float)local.X, (float)local.Y, (float)Bounds.Width, (float)Bounds.Height);
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
