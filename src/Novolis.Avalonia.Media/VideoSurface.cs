using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Novolis.Media.Rtc;

namespace Novolis.Avalonia.Media;

/// <summary>Displays <see cref="VideoFrame"/> samples on a <see cref="WriteableBitmap"/>.</summary>
public sealed class VideoSurface : Control
{
    WriteableBitmap? _bitmap;
    readonly object _gate = new();

    /// <summary>Optional caption drawn under the video.</summary>
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<VideoSurface, string?>(nameof(Label));

    /// <summary>Gets or sets the optional caption under the video.</summary>
    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>Presents a frame on the UI thread.</summary>
    public void Present(VideoFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (Dispatcher.UIThread.CheckAccess())
            PresentCore(frame);
        else
            Dispatcher.UIThread.Post(() => PresentCore(frame));
    }

    /// <summary>Clears the current bitmap.</summary>
    public void Clear()
    {
        if (Dispatcher.UIThread.CheckAccess())
            ClearCore();
        else
            Dispatcher.UIThread.Post(ClearCore);
    }

    void PresentCore(VideoFrame frame)
    {
        lock (_gate)
        {
            var fmt = frame.Format == VideoPixelFormat.Bgra32
                ? PixelFormats.Bgra8888
                : PixelFormats.Bgr24;
            if (_bitmap is null
                || _bitmap.PixelSize.Width != frame.Width
                || _bitmap.PixelSize.Height != frame.Height
                || _bitmap.Format != fmt)
            {
                _bitmap?.Dispose();
                _bitmap = new WriteableBitmap(
                    new PixelSize(frame.Width, frame.Height),
                    new Vector(96, 96),
                    fmt,
                    AlphaFormat.Opaque);
            }

            using var locked = _bitmap.Lock();
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
                        Buffer.MemoryCopy(src + y * srcStride, dest + y * destStride, destStride, copyWidth);
                    }
                }
            }
        }

        InvalidateVisual();
    }

    void ClearCore()
    {
        lock (_gate)
        {
            _bitmap?.Dispose();
            _bitmap = null;
        }

        InvalidateVisual();
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        WriteableBitmap? bmp;
        lock (_gate)
            bmp = _bitmap;

        var bounds = Bounds;
        if (bmp is not null)
        {
            context.DrawImage(bmp, new Rect(0, 0, bounds.Width, Math.Max(0, bounds.Height - 18)));
        }
        else
        {
            context.FillRectangle(Brushes.Black, new Rect(bounds.Size));
        }

        var label = Label;
        if (!string.IsNullOrWhiteSpace(label))
        {
            var text = new FormattedText(
                label,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                12,
                Brushes.White);
            context.DrawText(text, new Point(4, Math.Max(0, bounds.Height - 16)));
        }
    }

    /// <summary>Clears the bitmap when the control leaves the visual tree.</summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ClearCore();
        base.OnDetachedFromVisualTree(e);
    }
}
