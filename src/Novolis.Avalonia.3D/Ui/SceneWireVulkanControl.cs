using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Novolis.Avalonia._3D.Services;
using Novolis.Avalonia._3D.Session;
using Novolis.Avalonia.Rendering;
using Novolis.Math.Geometry;
using Novolis.Rendering.Backends.Vulkan;

namespace Novolis.Avalonia._3D.Ui;

/// <summary>
/// Avalonia CAD wireframe viewport driven by <see cref="VulkanWireframeRenderer"/>
/// (offscreen Vulkan graphics → <see cref="Rgba32FrameControl"/>).
/// </summary>
public sealed class SceneWireVulkanControl : Panel
{
    private readonly SceneSessionService _session;
    private readonly SceneViewportCamera _camera;
    private readonly ViewportFrameMeter _meter;
    private readonly Rgba32FrameControl _frame = new();
    private readonly TextBlock _errorText = new()
    {
        Foreground = Brushes.Orange,
        Margin = new Thickness(12),
        TextWrapping = TextWrapping.Wrap,
        IsVisible = false,
    };
    private readonly DispatcherTimer _timer;
    private readonly List<VulkanWireVertex> _lines = new(4096);
    private readonly List<WireSegment> _segments = new(4096);
    private readonly Stopwatch _sw = new();
    private VulkanWireframeRenderer? _renderer;
    private Rgba32[] _pixels = [];
    private int _w;
    private int _h;
    private string? _error;
    private bool _initAttempted;
    private bool _presenting;
    private bool _dirty = true;

    public SceneWireVulkanControl(
        SceneSessionService session,
        SceneViewportCamera camera,
        ViewportFrameMeter? meter = null)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        _meter = meter ?? new ViewportFrameMeter();
        Background = new SolidColorBrush(Color.FromRgb(18, 24, 32));
        Children.Add(_frame);
        Children.Add(_errorText);
        // ~20 Hz cap — full GPU render + CPU readback is too heavy for 60 Hz on the UI thread.
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Background, (_, _) => SafePresent());
    }

    public SceneViewportCamera Camera => _camera;
    public ViewportFrameMeter FrameMeter => _meter;

    /// <summary>Init/render error when Vulkan is unavailable.</summary>
    public string? LastError => _error;

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    /// <summary>Marks the surface dirty; present runs on the timer (never sync on the UI orbit path).</summary>
    public void RequestPresent() => _dirty = true;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Stop();
        _renderer?.Dispose();
        _renderer = null;
        _initAttempted = false;
        base.OnDetachedFromVisualTree(e);
    }

    private void SafePresent()
    {
        if (_presenting)
            return;
        if (!_dirty && _renderer is not null)
            return;

        _presenting = true;
        try
        {
            Present();
            _dirty = false;
        }
        catch (Exception ex)
        {
            ShowError(ex.ToString());
            _meter.Record(0, _camera.CameraInteracting);
            Debug.WriteLine($"Vulkan SafePresent failed: {ex}");
        }
        finally
        {
            _presenting = false;
        }
    }

    private void Present()
    {
        _sw.Restart();
        EnsureRenderer();
        if (_renderer is null)
        {
            ShowError(_error ?? "Vulkan unavailable.");
            _sw.Stop();
            _meter.Record(_sw.Elapsed.TotalMilliseconds, _camera.CameraInteracting);
            return;
        }

        var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        // Cap resolution — readback cost scales with pixels and runs on the UI thread.
        var maxEdge = _camera.CameraInteracting ? 960 : 1280;
        var w = System.Math.Clamp((int)System.Math.Round(Bounds.Width * scale), 64, maxEdge);
        var h = System.Math.Clamp((int)System.Math.Round(Bounds.Height * scale), 64, maxEdge);
        if (w != _w || h != _h || _pixels.Length != w * h)
        {
            _w = w;
            _h = h;
            _pixels = new Rgba32[w * h];
            try
            {
                _renderer.Resize(w, h);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                _sw.Stop();
                _meter.Record(_sw.Elapsed.TotalMilliseconds, _camera.CameraInteracting);
                return;
            }
        }

        _camera.SyncActiveCamera();
        var mvp = _camera.BuildViewProjection(w / (float)h);
        BuildLines();

        try
        {
            _renderer.Render(CollectionsMarshal.AsSpan(_lines), mvp, new Rgba32(18, 24, 32, 255));
            _renderer.Readback(_pixels);
            _frame.PresentCpuFrame(_pixels, _w, _h);
            ShowError(null);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
            Debug.WriteLine($"Vulkan present failed: {ex}");
        }

        _sw.Stop();
        _meter.Record(_sw.Elapsed.TotalMilliseconds, _camera.CameraInteracting);
        // If orbit is still moving, schedule another pass without stacking sync work.
        if (_camera.CameraInteracting)
            _dirty = true;
    }

    private void ShowError(string? message)
    {
        _error = message;
        if (string.IsNullOrWhiteSpace(message))
        {
            _errorText.IsVisible = false;
            return;
        }

        _errorText.Text = message;
        _errorText.IsVisible = true;
    }

    private void EnsureRenderer()
    {
        if (_renderer is not null || _initAttempted)
            return;
        _initAttempted = true;
        try
        {
            if (VulkanWireframeRenderer.TryCreate(out var renderer) && renderer is not null)
            {
                _renderer = renderer;
                return;
            }

            _error = "VulkanWireframeRenderer.TryCreate returned false.";
        }
        catch (Exception ex)
        {
            _error = ex.ToString();
        }
    }

    private void BuildLines()
    {
        _lines.Clear();
        WireSceneLineBuilder.Build(_session, _segments);
        foreach (var seg in _segments)
        {
            _lines.Add(VulkanWireVertex.From(seg.A, seg.R, seg.G, seg.Blue));
            _lines.Add(VulkanWireVertex.From(seg.B, seg.R, seg.G, seg.Blue));
        }
    }
}
