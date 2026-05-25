using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Novolis.Math.Geometry;
using Novolis.Rendering.Presentation.Abstractions;

namespace Novolis.Avalonia.Rendering;

/// <summary>
/// Displays CPU path-traced or software-rendered RGBA frames (implements the same contract as <see cref="IFramePresenter"/>).
/// </summary>
public class Rgba32FrameControl : Control, IFramePresenter
{
    private WriteableBitmap? _bitmap;
    private Image? _image;

    /// <inheritdoc />
    public void PresentCpuFrame(ReadOnlySpan<Rgba32> pixels, int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var copy = pixels.ToArray();
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

            Rgba32Bitmap.CopyPixels(_bitmap, copy, width, height);
            InvalidateVisual();
        }, DispatcherPriority.Render);
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        VisualChildren.Clear();
        _image = null;
        _bitmap = null;
        base.OnDetachedFromVisualTree(e);
    }
}
