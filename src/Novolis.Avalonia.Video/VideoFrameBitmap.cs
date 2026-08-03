using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Novolis.Video.Rtc;

namespace Novolis.Avalonia.Video;

/// <summary>Converts <see cref="VideoFrame"/> samples to Avalonia bitmaps for library previews.</summary>
public static class VideoFrameBitmap
{
    /// <summary>Creates a BGRA WriteableBitmap copy of <paramref name="frame"/>.</summary>
    public static WriteableBitmap ToBitmap(VideoFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        var fmt = frame.Format == VideoPixelFormat.Bgra32
            ? PixelFormats.Bgra8888
            : PixelFormats.Bgr24;
        var bmp = new WriteableBitmap(
            new PixelSize(frame.Width, frame.Height),
            new Vector(96, 96),
            fmt,
            AlphaFormat.Opaque);
        using var locked = bmp.Lock();
        var copy = Math.Min(frame.Pixels.Length, locked.RowBytes * frame.Height);
        unsafe
        {
            fixed (byte* src = frame.Pixels)
            {
                Buffer.MemoryCopy(src, (void*)locked.Address, copy, copy);
            }
        }

        return bmp;
    }
}
