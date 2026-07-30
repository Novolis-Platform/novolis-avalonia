using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Novolis.Avalonia._3D.Services;
using Novolis.Avalonia._3D.Session;

namespace Novolis.Avalonia._3D.Ui;

internal interface ISceneWireGlGpu : IDisposable
{
    void Render(SceneSessionService session, SceneViewportCamera camera, int framebuffer, int w, int h);
}

/// <summary>Native Avalonia OpenGL wireframe viewport — preferred over Raylib streaming.</summary>
/// <remarks>
/// Do not put Silk.NET types on this class. Loading Silk before Avalonia finishes
/// <see cref="OpenGlControlBase"/> context setup prevents <see cref="OnOpenGlInit"/> from running.
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

    public SceneWireGlControl(
        SceneSessionService session,
        SceneViewportCamera camera,
        ViewportFrameMeter? meter = null)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        _meter = meter ?? new ViewportFrameMeter();
    }

    public SceneViewportCamera Camera => _camera;
    public ViewportFrameMeter FrameMeter => _meter;
    public string? LastError => _glError;

    public void RequestPresent()
    {
        if (_glReady)
            RequestNextFrameRendering();
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        try
        {
            _gpu = SceneWireGlBootstrap.Create(gl);
            _glError = null;
            _glReady = true;
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
            RequestNextFrameRendering();
            return;
        }

        _sw.Restart();
        try
        {
            var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
            var w = System.Math.Max(1, (int)System.Math.Round(Bounds.Width * scale));
            var h = System.Math.Max(1, (int)System.Math.Round(Bounds.Height * scale));
            _gpu.Render(_session, _camera, framebuffer, w, h);
            _glError = null;
        }
        catch (Exception ex)
        {
            _glError = ex.Message;
            Debug.WriteLine($"SceneWireGlControl render failed: {ex}");
        }

        _sw.Stop();
        _meter.Record(_sw.Elapsed.TotalMilliseconds, _camera.CameraInteracting);
        RequestNextFrameRendering();
    }
}
