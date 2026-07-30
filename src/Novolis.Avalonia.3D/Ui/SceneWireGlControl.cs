using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using Novolis.Avalonia._3D.Services;
using Novolis.Avalonia._3D.Session;

namespace Novolis.Avalonia._3D.Ui;

internal interface ISceneWireGlGpu : IDisposable
{
    void Render(SceneSessionService session, SceneViewportCamera camera, int framebuffer, int w, int h, bool rebuildLines);
    void ReadRgba(Span<byte> rgba, int w, int h);
}

/// <summary>Native Avalonia OpenGL wireframe viewport — preferred over Raylib streaming.</summary>
/// <remarks>
/// Do not put Silk.NET types on this class. Loading Silk before Avalonia finishes
/// <see cref="OpenGlControlBase"/> context setup prevents <see cref="OnOpenGlInit"/> from running.
/// Dirty-driven present only — continuous RequestNextFrameRendering causes flicker on large meshes.
/// </remarks>
public sealed class SceneWireGlControl : OpenGlControlBase
{
    private readonly SceneSessionService _session;
    private readonly SceneViewportCamera _camera;
    private readonly ViewportFrameMeter _meter;
    private readonly Stopwatch _sw = new();
    private ISceneWireGlGpu? _gpu;
    private string? _glError;
    private bool _glReady;
    private bool _dirty = true;
    private bool _linesDirty = true;
    private string? _capturePath;
    private TaskCompletionSource<bool>? _captureTcs;

    public SceneWireGlControl(
        SceneSessionService session,
        SceneViewportCamera camera,
        ViewportFrameMeter? meter = null)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        _meter = meter ?? new ViewportFrameMeter();
        _session.DocumentChanged += () =>
        {
            _linesDirty = true;
            RequestPresent();
        };
        _camera.Changed += RequestPresent;
    }

    public SceneViewportCamera Camera => _camera;
    public ViewportFrameMeter FrameMeter => _meter;
    public string? LastError => _glError;

    public void RequestPresent()
    {
        _dirty = true;
        if (_glReady)
            RequestNextFrameRendering();
    }

    /// <summary>Captures the next GL frame as PNG (readback). Completes false on failure.</summary>
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
            _gpu = SceneWireGlBootstrap.Create(gl);
            _glError = null;
            _glReady = true;
            _dirty = true;
            _linesDirty = true;
            RequestNextFrameRendering();
        }
        catch (Exception ex)
        {
            _glReady = false;
            _glError = ex.Message;
            Debug.WriteLine($"SceneWireGlControl init failed: {ex}");
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
        {
            if (!string.IsNullOrWhiteSpace(_glError))
                _meter.Record(0, _camera.CameraInteracting);
            return;
        }

        if (!_dirty && !_camera.CameraInteracting && _capturePath is null)
            return;

        _sw.Restart();
        try
        {
            var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
            var w = System.Math.Max(1, (int)System.Math.Round(Bounds.Width * scale));
            var h = System.Math.Max(1, (int)System.Math.Round(Bounds.Height * scale));
            var rebuild = _linesDirty;
            if (rebuild)
                _linesDirty = false;

            _gpu.Render(_session, _camera, framebuffer, w, h, rebuild);
            _glError = null;
            _dirty = _camera.CameraInteracting;

            if (_capturePath is { } path)
            {
                var ok = TryWriteCapture(path, w, h);
                _capturePath = null;
                var tcs = _captureTcs;
                _captureTcs = null;
                tcs?.TrySetResult(ok);
            }
        }
        catch (Exception ex)
        {
            _glError = ex.Message;
            Debug.WriteLine($"SceneWireGlControl render failed: {ex}");
            _captureTcs?.TrySetResult(false);
            _capturePath = null;
            _captureTcs = null;
        }

        _sw.Stop();
        _meter.Record(_sw.Elapsed.TotalMilliseconds, _camera.CameraInteracting);

        // Keep presenting only while orbiting — idle holds the last frame (no flicker).
        if (_dirty || _camera.CameraInteracting)
            RequestNextFrameRendering();
    }

    private bool TryWriteCapture(string path, int w, int h)
    {
        try
        {
            if (_gpu is null || w < 2 || h < 2)
                return false;
            var rgba = new byte[w * h * 4];
            _gpu.ReadRgba(rgba, w, h);
            // GL is bottom-up; flip for PNG.
            var flipped = new byte[rgba.Length];
            var stride = w * 4;
            for (var y = 0; y < h; y++)
                Buffer.BlockCopy(rgba, y * stride, flipped, (h - 1 - y) * stride, stride);

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var bmp = new global::Avalonia.Media.Imaging.WriteableBitmap(
                new PixelSize(w, h),
                new Vector(96, 96),
                global::Avalonia.Platform.PixelFormat.Rgba8888,
                global::Avalonia.Platform.AlphaFormat.Opaque);
            using (var fb = bmp.Lock())
            {
                System.Runtime.InteropServices.Marshal.Copy(flipped, 0, fb.Address, flipped.Length);
            }

            using var stream = File.Create(path);
            bmp.Save(stream);
            return stream.Length > 32;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GL capture failed: {ex}");
            return false;
        }
    }
}
