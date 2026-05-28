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
/// Avalonia control that hosts a Raylib viewport (hidden GLFW window + RGBA streaming).
/// Handle <see cref="FrameRendering"/> to draw with Raylib APIs on the render thread.
/// </summary>
public class RaylibHostControl : Control
{
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

    private readonly DispatcherTimer _presentTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly HostFrameRenderer _renderer = new();
    private RaylibHostSession? _session;
    private WriteableBitmap? _bitmap;
    private Image? _image;

    /// <summary>Creates the control.</summary>
    public RaylibHostControl()
    {
        _presentTimer.Tick += (_, _) => PresentLatestFrame();
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

    /// <summary>
    /// Raised on the Raylib render thread between <c>BeginDrawing</c> and <c>EndDrawing</c>.
    /// Only invoke Raylib draw APIs from this handler.
    /// </summary>
    public event EventHandler<RaylibFrameEventArgs>? FrameRendering;

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        StartHost();
        _presentTimer.Start();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _presentTimer.Stop();
        StopHost();
        VisualChildren.Clear();
        _image = null;
        _bitmap = null;
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
            if (IsHostRunning)
            {
                RestartHost();
            }
        }
    }

    private void StartHost()
    {
        StopHost();

        var width = System.Math.Clamp(FrameWidth, 64, 4096);
        var height = System.Math.Clamp(FrameHeight, 64, 4096);
        var options = new RaylibEmbeddedOptions
        {
            Width = width,
            Height = height,
            TargetFps = System.Math.Clamp(TargetFps, 1, 240),
            WindowTitle = "Novolis.Avalonia.Raylib",
        };

        _renderer.Control = this;
        _session = new RaylibHostSession(options, _renderer);
        _session.Start();
        IsHostRunning = true;
    }

    private void RestartHost()
    {
        if (VisualRoot is null)
        {
            return;
        }

        StartHost();
    }

    private void StopHost()
    {
        _session?.Dispose();
        _session = null;
        _renderer.Control = null;
        IsHostRunning = false;
    }

    private void PresentLatestFrame()
    {
        if (_session is null || !_session.TryTakeFrame(out var pixels, out var width, out var height))
        {
            return;
        }

        if (width <= 0 || height <= 0)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (_bitmap is null || _bitmap.PixelSize.Width != width || _bitmap.PixelSize.Height != height)
            {
                _bitmap = Rgba32Bitmap.CreateBitmap(width, height);
                _image ??= new Image
                {
                    Stretch = Stretch.Fill,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                };
                _image.Source = _bitmap;
                if (_image.Parent is null)
                {
                    VisualChildren.Add(_image);
                }
            }

            Rgba32Bitmap.CopyPixels(_bitmap, pixels, width, height);
            InvalidateVisual();
        }, DispatcherPriority.Render);
    }

    private sealed class HostFrameRenderer : IRaylibFrameRenderer
    {
        public RaylibHostControl? Control { get; set; }

        public void OnFrame(float deltaSeconds, int screenWidth, int screenHeight)
        {
            var control = Control;
            if (control is null)
            {
                return;
            }

            control.FrameRendering?.Invoke(control, new RaylibFrameEventArgs(deltaSeconds, screenWidth, screenHeight));
        }
    }
}
