using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Rendering;

/// <summary>Copies <see cref="Rgba32"/> frames into Avalonia <see cref="WriteableBitmap"/> (BGRA8888).</summary>
public static class Rgba32Bitmap
{
    /// <summary>Creates or resizes a writeable bitmap for the given dimensions.</summary>
    public static WriteableBitmap CreateBitmap(int width, int height) =>
        new(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);

    /// <summary>Writes one <see cref="Rgba32"/> pixel into four BGRA bytes.</summary>
    public static void WriteBgraPixel(Span<byte> destination, Rgba32 pixel)
    {
        destination[0] = pixel.B;
        destination[1] = pixel.G;
        destination[2] = pixel.R;
        destination[3] = pixel.A;
    }

    /// <summary>Uploads RGBA pixels into a BGRA writeable bitmap.</summary>
    public static void CopyPixels(WriteableBitmap bitmap, ReadOnlySpan<Rgba32> pixels, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        if (pixels.Length < width * height)
            throw new ArgumentException("Pixel span is too small for the given dimensions.", nameof(pixels));

        using var frame = bitmap.Lock();
        CopyPixelsToLockedFrame(frame, pixels, width, height);
    }

    /// <summary>Uploads RGBA pixels into a locked framebuffer.</summary>
    public static unsafe void CopyPixelsToLockedFrame(ILockedFramebuffer frame, ReadOnlySpan<Rgba32> pixels, int width, int height)
    {
        var dstBase = (byte*)frame.Address;
        var stride = frame.RowBytes;
        for (var y = 0; y < height; y++)
        {
            var rowSrc = pixels.Slice(y * width, width);
            var rowDst = dstBase + y * stride;
            for (var x = 0; x < width; x++)
            {
                var p = rowSrc[x];
                var offset = x * 4;
                rowDst[offset] = p.B;
                rowDst[offset + 1] = p.G;
                rowDst[offset + 2] = p.R;
                rowDst[offset + 3] = p.A;
            }
        }
    }
}
