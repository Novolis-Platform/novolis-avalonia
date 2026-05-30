using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Novolis.Avalonia.Rendering;
using Novolis.Math.Geometry;
using Novolis.Raylib.Abstractions;
using Novolis.Raylib.Shell;

namespace Novolis.Avalonia.Raylib;

/// <summary>
/// Avalonia panel that hosts a Raylib viewport (hidden GLFW window + RGBA streaming).
/// Handle <see cref="FrameRendering"/> to draw with Raylib APIs on the render thread.
/// </summary>
public class RaylibHostControl : Panel
{
    private static readonly TimeSpan ResizeDebounce = TimeSpan.FromMilliseconds(150);

    /// <summary>Internal render width in pixels.</summary>
    public static readonly StyledProperty<int> FrameWidthProperty =
        AvaloniaProperty.Register<RaylibHostControl, int>(nameof(FrameWidth), 640);

    /// <summary>Internal render height in pixels.</summary>
    public static readonly StyledProperty<int> FrameHeightProperty =
        AvaloniaProperty.Register<RaylibHostControl, int>(nameof(FrameHeight), 480);

    /// <summary>Target frames per second for the embedded Raylib loop.</summary>
    public static readonly StyledProperty<int> TargetFpsProperty =
        AvaloniaProperty.Register<RaylibHostControl, int>(nameof(TargetFps), 60);

    /// <summary>Whether the Raylib host loop is running.</summary>
    public static readonly StyledProperty<bool> IsHostRunningProperty =
        AvaloniaProperty.Register<RaylibHostControl, bool>(nameof(IsHostRunning));

    private readonly Image _image;
    private readonly HostFrameRenderer _renderer = new();
    private readonly DispatcherTimer _presentTimer;
    private readonly DispatcherTimer _resizeDebounceTimer;
    private RaylibHostSession? _session;
    private WriteableBitmap? _bitmap;
    private Rgba32[]? _presentScratch;
    private bool _hostActiveRequested;

    /// <summary>Creates the control.</summary>
    public RaylibHostControl()
    {
        Background = new SolidColorBrush(Color.FromRgb(24, 24, 28));
        _image = new Image
        {
            Stretch = Stretch.Fill,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        Children.Add(_image);
        _presentTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render, (_, _) => PresentLatestFrame());
        _resizeDebounceTimer = new DispatcherTimer(ResizeDebounce, DispatcherPriority.Background, (_, _) => ApplyDebouncedResize());
        LayoutUpdated += (_, _) =>
        {
            if (_hostActiveRequested)
                EnsureHostStarted();
        };
    }

    /// <summary>Internal render width in pixels.</summary>
    public int FrameWidth
    {
        get => GetValue(FrameWidthProperty);
        set => SetValue(FrameWidthProperty, value);
    }

    /// <summary>Internal render height in pixels.</summary>
    public int FrameHeight
    {
        get => GetValue(FrameHeightProperty);
        set => SetValue(FrameHeightProperty, value);
    }

    /// <summary>Target frames per second for the embedded Raylib loop.</summary>
    public int TargetFps
    {
        get => GetValue(TargetFpsProperty);
        set => SetValue(TargetFpsProperty, value);
    }

    /// <summary>Whether the Raylib host loop is running.</summary>
    public bool IsHostRunning
    {
        get => GetValue(IsHostRunningProperty);
        private set => SetValue(IsHostRunningProperty, value);
    }

    /// <summary>Milliseconds since the last captured frame, or <c>-1</c> when none yet.</summary>
    public double LastFrameAgeMs
    {
        get
        {
            if (_session is null || _session.LastFrameAt == DateTimeOffset.MinValue)
                return -1;
            return (DateTimeOffset.UtcNow - _session.LastFrameAt).TotalMilliseconds;
        }
    }

    /// <summary>
    /// Raised on the Raylib render thread between <c>BeginDrawing</c> and <c>EndDrawing</c>.
    /// Only invoke Raylib draw APIs from this handler.
    /// </summary>
    public event EventHandler<RaylibFrameEventArgs>? FrameRendering;

    /// <summary>Queues a redraw on the render thread (on-demand pipeline).</summary>
    public void RequestFrame()
    {
        if (_session is { IsRunning: true })
            _session.RequestRedraw();
    }

    /// <summary>Starts the host when attached, sized, and active; safe to call repeatedly.</summary>
    public void EnsureHostStarted()
    {
        if (!_hostActiveRequested || VisualRoot is null)
            return;

        var width = System.Math.Clamp(FrameWidth, 64, 4096);
        var height = System.Math.Clamp(FrameHeight, 64, 4096);
        if (width < 64 || height < 64)
            return;

        if (_session is { IsRunning: true })
        {
            RequestFrame();
            return;
        }

        StartHost();
    }

    /// <summary>Starts or stops the embedded Raylib loop (only one GLFW host per process).</summary>
    public void SetHostActive(bool active)
    {
        _hostActiveRequested = active;
        if (active)
        {
            EnsureHostStarted();
            if (IsHostRunning)
                _presentTimer.Start();
            else
                Dispatcher.UIThread.Post(EnsureHostStarted, DispatcherPriority.Loaded);
        }
        else
        {
            _presentTimer.Stop();
            _resizeDebounceTimer.Stop();
            StopHost();
        }
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        SetHostActive(true);
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        SetHostActive(false);
        _bitmap = null;
        _presentScratch = null;
        base.OnDetachedFromVisualTree(e);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FrameWidthProperty
            || change.Property == FrameHeightProperty
            || change.Property == TargetFpsProperty)
        {
            if (!IsHostRunning)
                return;

            _resizeDebounceTimer.Stop();
            _resizeDebounceTimer.Start();
        }
    }

    private void ApplyDebouncedResize()
    {
        _resizeDebounceTimer.Stop();
        if (!IsHostRunning || _session is null)
            return;

        _session.RequestResize(FrameWidth, FrameHeight, TargetFps);
        RequestFrame();
    }

    private void StartHost()
    {
        StopHost();

        var options = new RaylibEmbeddedOptions
        {
            Width = System.Math.Clamp(FrameWidth, 64, 4096),
            Height = System.Math.Clamp(FrameHeight, 64, 4096),
            TargetFps = System.Math.Clamp(TargetFps, 1, 240),
            WindowTitle = "Novolis.Avalonia.Raylib",
        };

        _renderer.Control = this;
        _session = new RaylibHostSession(options, _renderer);
        _session.Start();
        IsHostRunning = true;
        if (_hostActiveRequested)
            _presentTimer.Start();
    }

    private void StopHost()
    {
        _presentTimer.Stop();
        _session?.Dispose();
        _session = null;
        _renderer.Control = null;
        IsHostRunning = false;
    }

    private void PresentLatestFrame()
    {
        if (_session is null)
            return;

        if (!_session.TryReadFrame(out var packet) || packet is null)
            return;

        if (packet.Width <= 0 || packet.Height <= 0)
            return;

        if (Dispatcher.UIThread.CheckAccess())
            ApplyFrame(packet.Pixels, packet.Width, packet.Height);
        else
            Dispatcher.UIThread.Post(() => ApplyFrame(packet.Pixels, packet.Width, packet.Height), DispatcherPriority.Render);
    }

    private void ApplyFrame(Rgba32[] pixels, int width, int height)
    {
        if (_bitmap is null || _bitmap.PixelSize.Width != width || _bitmap.PixelSize.Height != height)
        {
            _bitmap = Rgba32Bitmap.CreateBitmap(width, height);
            _image.Source = _bitmap;
        }

        if (_presentScratch is null || _presentScratch.Length != pixels.Length)
            _presentScratch = new Rgba32[pixels.Length];

        pixels.AsSpan().CopyTo(_presentScratch);
        Rgba32Bitmap.CopyPixels(_bitmap, _presentScratch, width, height);
        InvalidateVisual();
    }

    private sealed class HostFrameRenderer : IRaylibFrameRenderer
    {
        public RaylibHostControl? Control { get; set; }

        public void OnFrame(float deltaSeconds, int screenWidth, int screenHeight)
        {
            var control = Control;
            control?.FrameRendering?.Invoke(control, new RaylibFrameEventArgs(deltaSeconds, screenWidth, screenHeight));
        }
    }
}
