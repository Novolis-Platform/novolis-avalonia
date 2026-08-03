using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Novolis.Video.Rtc;

namespace Novolis.Avalonia.Video;

/// <summary>Loads still images into BGRA32 <see cref="VideoFrame"/> for edit preview.</summary>
public static class AvaloniaStillLoader
{
    /// <summary>Decodes <paramref name="path"/> and scales into a BGRA32 frame.</summary>
    public static VideoFrame LoadBgra(string path, int targetWidth, int targetHeight)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetHeight);

        using var source = new Bitmap(path);
        var render = new RenderTargetBitmap(new PixelSize(targetWidth, targetHeight), new Vector(96, 96));
        using (var ctx = render.CreateDrawingContext(true))
        {
            ctx.DrawImage(
                source,
                new Rect(0, 0, source.PixelSize.Width, source.PixelSize.Height),
                new Rect(0, 0, targetWidth, targetHeight));
        }

        var stride = targetWidth * 4;
        var pixels = new byte[stride * targetHeight];
        unsafe
        {
            fixed (byte* dst = pixels)
            {
                render.CopyPixels(
                    new PixelRect(0, 0, targetWidth, targetHeight),
                    (nint)dst,
                    pixels.Length,
                    stride);
            }
        }

        render.Dispose();
        return new VideoFrame(targetWidth, targetHeight, stride, VideoPixelFormat.Bgra32, pixels);
    }
}
