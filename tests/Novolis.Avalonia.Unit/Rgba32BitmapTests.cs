using Novolis.Avalonia.Rendering;
using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Unit;

public sealed class Rgba32BitmapTests
{
    [Test]
    public async Task WriteBgraPixel_MapsRgbaToBgraOrder()
    {
        var dest = new byte[4];
        Rgba32Bitmap.WriteBgraPixel(dest, new Rgba32(255, 128, 64, 200));
        await Assert.That(dest[0]).IsEqualTo((byte)64);
        await Assert.That(dest[1]).IsEqualTo((byte)128);
        await Assert.That(dest[2]).IsEqualTo((byte)255);
        await Assert.That(dest[3]).IsEqualTo((byte)200);
    }
}
