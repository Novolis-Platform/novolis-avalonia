using Novolis.Avalonia.Map;
using Novolis.IO.Maps;
using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Unit.Map;

public sealed class RasterMapTileSourceTests
{
    [Test]
    public async Task RasterMapTileSource_exposes_provider_metadata()
    {
        var adapter = new RasterMapTileSource(new EncodedTileSource());

        await Assert.That(adapter.Template.Name).IsEqualTo("test-map");
        await Assert.That(adapter.Attribution).IsEqualTo("Test");
    }

    sealed class EncodedTileSource : IMapRasterSource
    {
        public XyzMapTemplate Template { get; } = new(
            "test-map",
            "https://maps.example/{z}/{x}/{y}.png",
            "Test");

        public ValueTask<MapRasterTile?> GetTileAsync(
            MapTileKey key,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<MapRasterTile?>(null);
    }
}
