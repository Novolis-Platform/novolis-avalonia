using System.Diagnostics;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Novolis.Avalonia._3D.Services;
using Novolis.Avalonia._3D.Session;
using Novolis.Avalonia.Rendering;
using Novolis.Math.Geometry;

namespace Novolis.Avalonia._3D.Ui;

/// <summary>CPU software wireframe into <see cref="Rgba32FrameControl"/> (no Raylib).</summary>
public sealed class SceneWireCpuControl : Panel
{
    private readonly SceneSessionService _session;
    private readonly SceneViewportCamera _camera;
    private readonly ViewportFrameMeter _meter;
    private readonly Rgba32FrameControl _frame = new();
    private readonly DispatcherTimer _timer;
    private readonly List<WireSegment> _segments = new(4096);
    private readonly Stopwatch _sw = new();
    private Rgba32[] _pixels = [];
    private int _w;
    private int _h;

    public SceneWireCpuControl(
        SceneSessionService session,
        SceneViewportCamera camera,
        ViewportFrameMeter? meter = null)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        _meter = meter ?? new ViewportFrameMeter();
        Children.Add(_frame);
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(33), DispatcherPriority.Render, (_, _) => Present());
    }

    public SceneViewportCamera Camera => _camera;
    public ViewportFrameMeter FrameMeter => _meter;

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Stop();
        base.OnDetachedFromVisualTree(e);
    }

    private void Present()
    {
        _sw.Restart();
        var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        var w = System.Math.Clamp((int)System.Math.Round(Bounds.Width * scale), 64, 2048);
        var h = System.Math.Clamp((int)System.Math.Round(Bounds.Height * scale), 64, 2048);
        if (w != _w || h != _h || _pixels.Length != w * h)
        {
            _w = w;
            _h = h;
            _pixels = new Rgba32[w * h];
        }

        var bg = new Rgba32(18, 24, 32, 255);
        Array.Fill(_pixels, bg);

        _camera.SyncActiveCamera();
        var mvp = _camera.BuildViewProjection(w / (float)h);
        WireSceneLineBuilder.Build(_session, _segments);

        var drawn = 0;
        foreach (var seg in _segments)
        {
            if (drawn++ > 120_000)
                break;
            DrawWorldLine(mvp, seg.A, seg.B, new Rgba32(seg.R, seg.G, seg.Blue, 255));
        }

        _frame.PresentCpuFrame(_pixels, _w, _h);
        _sw.Stop();
        _meter.Record(_sw.Elapsed.TotalMilliseconds, _camera.CameraInteracting);
    }

    private void DrawWorldLine(Matrix4x4 mvp, Vector3 a, Vector3 b, Rgba32 color)
    {
        if (!Project(mvp, a, out var ax, out var ay, out var az) || !Project(mvp, b, out var bx, out var by, out var bz))
            return;
        if (az < 0 || bz < 0)
            return;
        Bresenham(ax, ay, bx, by, color);
    }

    private bool Project(Matrix4x4 mvp, Vector3 p, out int x, out int y, out float z)
    {
        var clip = Vector4.Transform(new Vector4(p, 1f), mvp);
        if (MathF.Abs(clip.W) < 1e-6f)
        {
            x = y = 0;
            z = -1;
            return false;
        }

        var ndc = clip / clip.W;
        x = (int)((ndc.X * 0.5f + 0.5f) * (_w - 1));
        y = (int)((1f - (ndc.Y * 0.5f + 0.5f)) * (_h - 1));
        z = ndc.Z;
        return true;
    }

    private void Bresenham(int x0, int y0, int x1, int y1, Rgba32 color)
    {
        var dx = System.Math.Abs(x1 - x0);
        var sx = x0 < x1 ? 1 : -1;
        var dy = -System.Math.Abs(y1 - y0);
        var sy = y0 < y1 ? 1 : -1;
        var err = dx + dy;
        while (true)
        {
            Plot(x0, y0, color);
            if (x0 == x1 && y0 == y1)
                break;
            var e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x0 += sx;
            }

            if (e2 <= dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    private void Plot(int x, int y, Rgba32 color)
    {
        if ((uint)x >= (uint)_w || (uint)y >= (uint)_h)
            return;
        _pixels[y * _w + x] = color;
    }
}
