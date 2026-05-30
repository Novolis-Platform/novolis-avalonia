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
public class Rgba32FrameControl : Panel, IFramePresenter
{
    private readonly Image _image;
    private WriteableBitmap? _bitmap;
    private Rgba32[]? _staging;

    /// <summary>Creates the control with a fill-stretched image child.</summary>
    public Rgba32FrameControl()
    {
        Background = new SolidColorBrush(Color.FromRgb(24, 24, 28));
        _image = new Image
        {
            Stretch = Stretch.Fill,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        Children.Add(_image);
    }

    /// <inheritdoc />
    public void PresentCpuFrame(ReadOnlySpan<Rgba32> pixels, int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        if (pixels is Rgba32[] arr && Dispatcher.UIThread.CheckAccess())
        {
            ApplyFrame(arr, width, height);
            return;
        }

        var count = width * height;
        if (_staging is null || _staging.Length != count)
            _staging = new Rgba32[count];

        pixels.CopyTo(_staging);
        if (Dispatcher.UIThread.CheckAccess())
            ApplyFrame(_staging, width, height);
        else
            Dispatcher.UIThread.Post(() => ApplyFrame(_staging!, width, height), DispatcherPriority.Render);
    }

    private void ApplyFrame(Rgba32[] pixels, int width, int height)
    {
        if (_bitmap is null || _bitmap.PixelSize.Width != width || _bitmap.PixelSize.Height != height)
        {
            _bitmap = Rgba32Bitmap.CreateBitmap(width, height);
            _image.Source = _bitmap;
        }

        Rgba32Bitmap.CopyPixels(_bitmap, pixels, width, height);
        InvalidateVisual();
    }
}
