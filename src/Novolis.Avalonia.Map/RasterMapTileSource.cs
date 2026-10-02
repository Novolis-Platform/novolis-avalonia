using Avalonia.Media.Imaging;
using Novolis.IO.Maps;
using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Map;

/// <summary>Decodes provider-neutral raster bytes for <see cref="MapControl" />.</summary>
public sealed class RasterMapTileSource : IMapTileSource
{
    readonly IMapRasterSource _source;

    /// <summary>Creates an Avalonia tile source over an encoded raster source.</summary>
    public RasterMapTileSource(IMapRasterSource source) =>
        _source = source ?? throw new ArgumentNullException(nameof(source));

    /// <summary>Provider template used by the source.</summary>
    public XyzMapTemplate Template => _source.Template;

    /// <summary>Attribution required by the provider.</summary>
    public string Attribution => Template.Attribution;

    /// <inheritdoc />
    public async ValueTask<MapTile?> GetTileAsync(
        MapTileKey key,
        CancellationToken cancellationToken = default)
    {
        var raster = await _source.GetTileAsync(key, cancellationToken);
        if (raster is null)
            return null;

        await using var stream = new MemoryStream(raster.PngBytes, writable: false);
        return new MapTile(key, new Bitmap(stream));
    }
}
