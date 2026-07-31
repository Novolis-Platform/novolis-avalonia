using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using Novolis.Avalonia._3D.Services;
using Novolis.Avalonia._3D.Session;

namespace Novolis.Avalonia._3D.Ui;

/// <summary>OpenGL Lambert shaded preview for the Render popup (uses scene lights + ambient).</summary>
public sealed class SceneShadedGlControl : OpenGlControlBase
{
    private readonly SceneSessionService _session;
    private readonly SceneViewportCamera _camera;
    private readonly SceneRenderSettings _settings;
    private readonly Stopwatch _sw = new();
    private ISceneShadedGlGpu? _gpu;
    private string? _glError;
    private bool _glReady;
    private bool _dirty = true;
    private bool _meshDirty = true;
    private int _lastMeshGeneration = -1;
    private string? _capturePath;
    private TaskCompletionSource<bool>? _captureTcs;
    private Point? _last;
    private bool _orbiting;
    private bool _panning;

    public SceneShadedGlControl(SceneSessionService session, SceneViewportCamera? camera = null)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _camera = camera ?? new SceneViewportCamera(session) { FollowDocumentCamera = false };
        _camera.Orbit.MaxDistance = 400f;
        _camera.Orbit.MinPitch = -MathF.PI * 0.49f;
        _settings = session.RenderSettings;
        Focusable = true;
        ClipToBounds = true;

        _session.DocumentChanged += () =>
        {
            // Mesh VBO rebuild is expensive — only when mesh generation advances (not light tweaks).
            if (_session.Evaluator.MeshGeneration != _lastMeshGeneration)
                _meshDirty = true;
            RequestPresent();
        };
        _settings.Changed += RequestPresent;
        _camera.Changed += RequestPresent;

        PointerPressed += OnPressed;
        PointerMoved += OnMoved;
        PointerReleased += OnReleased;
        PointerWheelChanged += (_, e) =>
        {
            _camera.Zoom((float)e.Delta.Y);
            e.Handled = true;
        };
    }

    public SceneViewportCamera Camera => _camera;
    public string? LastError => _glError;

    public void Fit() => _camera.Fit();

    public void RequestPresent()
    {
        _dirty = true;
        if (_glReady)
            RequestNextFrameRendering();
    }

    public Task<bool> CapturePngAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _captureTcs?.TrySetResult(false);
        _capturePath = path;
        _captureTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        RequestPresent();
        return _captureTcs.Task;
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        try
        {
            _gpu = SceneShadedGlBootstrap.Create(gl);
            _glError = null;
            _glReady = true;
            _dirty = true;
            _meshDirty = true;
            RequestNextFrameRendering();
        }
        catch (Exception ex)
        {
            _glReady = false;
            _glError = ex.Message;
            Debug.WriteLine($"SceneShadedGlControl init failed: {ex}");
        }
    }

    protected override void OnOpenGlLost()
    {
        _glReady = false;
        _gpu?.Dispose();
        _gpu = null;
        base.OnOpenGlLost();
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        _glReady = false;
        _gpu?.Dispose();
        _gpu = null;
    }

    protected override void OnOpenGlRender(GlInterface gl, int framebuffer)
    {
        if (_gpu is null)
            return;

        if (!_dirty && !_camera.CameraInteracting && _capturePath is null)
            return;

        _dirty = false;
        _sw.Restart();
        var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        var w = System.Math.Max(1, (int)System.Math.Round(Bounds.Width * scale));
        var h = System.Math.Max(1, (int)System.Math.Round(Bounds.Height * scale));

        var rebuild = _meshDirty || _session.Evaluator.MeshGeneration != _lastMeshGeneration;
        if (rebuild)
        {
            _meshDirty = false;
            _lastMeshGeneration = _session.Evaluator.MeshGeneration;
        }
        try
        {
            _gpu.Render(_session, _camera, _settings, framebuffer, w, h, rebuild);
        }
        catch (Exception ex)
        {
            _glError = ex.Message;
            Debug.WriteLine($"SceneShadedGlControl render failed: {ex}");
        }

        _sw.Stop();

        if (_capturePath is not null)
        {
            var path = _capturePath;
            _capturePath = null;
            var tcs = _captureTcs;
            _captureTcs = null;
            var ok = false;
            try
            {
                ok = WritePng(path, w, h);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Shaded PNG capture failed: {ex}");
            }

            Dispatcher.UIThread.Post(() => tcs?.TrySetResult(ok));
        }
    }

    private bool WritePng(string path, int w, int h)
    {
        if (_gpu is null) return false;
        var rgba = new byte[w * h * 4];
        _gpu.ReadRgba(rgba, w, h);
        // GL origin is bottom-left — flip rows for PNG.
        var flipped = new byte[rgba.Length];
        var stride = w * 4;
        for (var y = 0; y < h; y++)
            Buffer.BlockCopy(rgba, y * stride, flipped, (h - 1 - y) * stride, stride);
        return SceneViewportExporter.TryWriteRgbaPng(path, flipped, w, h);
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        Focus();
        var pt = e.GetCurrentPoint(this);
        _last = e.GetPosition(this);
        if (pt.Properties.IsMiddleButtonPressed && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            _panning = true;
            _orbiting = false;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (pt.Properties.IsMiddleButtonPressed || pt.Properties.IsLeftButtonPressed || pt.Properties.IsRightButtonPressed)
        {
            _orbiting = true;
            _panning = false;
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if ((!_orbiting && !_panning) || _last is null) return;
        var pos = e.GetPosition(this);
        var dx = (float)(pos.X - _last.Value.X);
        var dy = (float)(pos.Y - _last.Value.Y);
        _last = pos;
        if (_panning)
            _camera.Pan(dx, dy);
        else
            _camera.OrbitDrag(dx, dy);
        e.Handled = true;
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_orbiting && !_panning) return;
        _orbiting = false;
        _panning = false;
        _last = null;
        e.Pointer.Capture(null);
        e.Handled = true;
    }
}
