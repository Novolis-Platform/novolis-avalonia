using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Novolis.Video;
using Novolis.Video.Rtc;

namespace Novolis.Avalonia.Video;

/// <summary>Displays video frames on a <see cref="WriteableBitmap"/>.</summary>
public sealed class VideoSurface : Control
{
    private WriteableBitmap? _bitmap;
    private readonly object _gate = new();
    private bool _letterbox;
    private int _videoWidth;
    private int _videoHeight;

    /// <summary>Optional caption drawn under the video.</summary>
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<VideoSurface, string?>(nameof(Label));

    /// <summary>
    /// When true, letterbox content to the top of the surface instead of
    /// centering it vertically.
    /// </summary>
    public static readonly StyledProperty<bool> AlignContentTopProperty =
        AvaloniaProperty.Register<VideoSurface, bool>(
            nameof(AlignContentTop),
            OperatingSystem.IsAndroid());

    /// <summary>Gets or sets the optional caption under the video.</summary>
    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>
    /// Gets or sets whether letterboxed content aligns to the top edge.
    /// </summary>
    public bool AlignContentTop
    {
        get => GetValue(AlignContentTopProperty);
        set => SetValue(AlignContentTopProperty, value);
    }

    /// <summary>Presents an RTC frame on the UI thread, filling the width.</summary>
    public void Present(VideoFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (Dispatcher.UIThread.CheckAccess())
            PresentRtcCore(frame);
        else
            Dispatcher.UIThread.Post(() => PresentRtcCore(frame));
    }

    /// <summary>Presents a raw BGRA frame and letterboxes it.</summary>
    public void Present(RawVideoFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (Dispatcher.UIThread.CheckAccess())
            PresentRawCore(frame);
        else
            Dispatcher.UIThread.Post(
                () => PresentRawCore(frame),
                DispatcherPriority.Background);
    }

    /// <summary>Clears the current bitmap.</summary>
    public void Clear()
    {
        if (Dispatcher.UIThread.CheckAccess())
            ClearCore();
        else
            Dispatcher.UIThread.Post(ClearCore);
    }

    private void PresentRtcCore(VideoFrame frame)
    {
        lock (_gate)
        {
            _letterbox = false;
            _videoWidth = frame.Width;
            _videoHeight = frame.Height;
            var fmt = frame.Format == Novolis.Video.Rtc.VideoPixelFormat.Bgra32
                ? PixelFormats.Bgra8888
                : PixelFormats.Bgr24;
            EnsureBitmap(frame.Width, frame.Height, fmt);
            using var locked = _bitmap!.Lock();
            var destStride = locked.RowBytes;
            var srcStride = frame.Stride;
            var height = frame.Height;
            var copyWidth = Math.Min(srcStride, destStride);
            unsafe
            {
                var dest = (byte*)locked.Address;
                fixed (byte* srcFixed = frame.Pixels)
                {
                    var src = srcFixed;
                    for (var y = 0; y < height; y++)
                    {
                        Buffer.MemoryCopy(
                            src + y * srcStride,
                            dest + y * destStride,
                            destStride,
                            copyWidth);
                    }
                }
            }
        }

        InvalidateVisual();
    }

    private void PresentRawCore(RawVideoFrame frame)
    {
        lock (_gate)
        {
            _letterbox = true;
            _videoWidth = frame.Width;
            _videoHeight = frame.Height;
            EnsureBitmap(
                frame.Width,
                frame.Height,
                PixelFormats.Bgra8888);
            using var locked = _bitmap!.Lock();
            var rowBytes = Math.Min(frame.Width * 4, frame.Stride);
            for (var row = 0; row < frame.Height; row++)
            {
                if (locked.Address == IntPtr.Zero)
                    throw new InvalidOperationException(
                        "The video bitmap was not writable.");

                Marshal.Copy(
                    frame.Pixels,
                    row * frame.Stride,
                    IntPtr.Add(locked.Address, row * locked.RowBytes),
                    rowBytes);
            }
        }

        InvalidateVisual();
    }

    private void EnsureBitmap(int width, int height, PixelFormat format)
    {
        if (_bitmap is not null
            && _bitmap.PixelSize.Width == width
            && _bitmap.PixelSize.Height == height
            && _bitmap.Format == format)
        {
            return;
        }

        _bitmap?.Dispose();
        _bitmap = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            format,
            AlphaFormat.Opaque);
    }

    private void ClearCore()
    {
        lock (_gate)
        {
            _bitmap?.Dispose();
            _bitmap = null;
            _letterbox = false;
            _videoWidth = 0;
            _videoHeight = 0;
        }

        InvalidateVisual();
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        WriteableBitmap? bmp;
        bool letterbox;
        int videoWidth;
        int videoHeight;
        lock (_gate)
        {
            bmp = _bitmap;
            letterbox = _letterbox;
            videoWidth = _videoWidth;
            videoHeight = _videoHeight;
        }

        var bounds = Bounds;
        if (bmp is null)
        {
            context.FillRectangle(Brushes.Black, new Rect(bounds.Size));
            DrawLabel(context, bounds);
            return;
        }

        if (letterbox)
        {
            var fit = VideoGeometry.CalculateFit(
                bounds.Width,
                bounds.Height,
                videoWidth,
                videoHeight,
                zoom: 1,
                panX: 0,
                panY: 0);
            if (fit.Scale <= 0)
                return;

            var originY = AlignContentTop ? 0 : fit.OriginY;
            var source = new Rect(
                new Size(bmp.PixelSize.Width, bmp.PixelSize.Height));
            var target = new Rect(
                fit.OriginX,
                originY,
                fit.RenderedWidth,
                fit.RenderedHeight);
            context.DrawImage(bmp, source, target);
        }
        else
        {
            context.DrawImage(
                bmp,
                new Rect(0, 0, bounds.Width, Math.Max(0, bounds.Height - 18)));
        }

        DrawLabel(context, bounds);
    }

    private void DrawLabel(DrawingContext context, Rect bounds)
    {
        var label = Label;
        if (string.IsNullOrWhiteSpace(label))
            return;

        var text = new FormattedText(
            label,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            12,
            Brushes.White);
        context.DrawText(text, new Point(4, Math.Max(0, bounds.Height - 16)));
    }

    /// <summary>Clears the bitmap when the control leaves the visual tree.</summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ClearCore();
        base.OnDetachedFromVisualTree(e);
    }
}
