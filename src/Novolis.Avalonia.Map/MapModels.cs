using Avalonia.Media.Imaging;
using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Map;

/// <summary>Map camera state in terrestrial coordinates.</summary>
public readonly record struct MapViewport
{
    /// <summary>Lowest supported world zoom.</summary>
    public const double MinimumZoom = 0;

    /// <summary>Highest supported world zoom.</summary>
    public const double MaximumZoom = 22;

    /// <summary>Creates a map viewport.</summary>
    public MapViewport(GeoCoordinate center, double zoom)
    {
        if (!double.IsFinite(zoom) || zoom is < MinimumZoom or > MaximumZoom)
            throw new ArgumentOutOfRangeException(
                nameof(zoom),
                zoom,
                $"Zoom must be between {MinimumZoom} and {MaximumZoom}.");

        Center = center;
        Zoom = zoom;
    }

    /// <summary>Geographic center of the viewport.</summary>
    public GeoCoordinate Center { get; }

    /// <summary>World zoom, allowing fractional values for smooth gestures.</summary>
    public double Zoom { get; }
}

/// <summary>A selectable point rendered by <see cref="MapControl" />.</summary>
public sealed record MapMarker(
    string Id,
    GeoCoordinate Position,
    string? Label = null,
    double RadiusPixels = 6);

/// <summary>A geographic circle rendered by <see cref="MapControl" />.</summary>
public sealed record MapCircleOverlay(
    string Id,
    GeoCircle Circle,
    string? Label = null);

/// <summary>A provider-neutral geographic track rendered as a connected line.</summary>
public sealed record MapTrackOverlay(
    string Id,
    IReadOnlyList<GeoCoordinate> Points,
    string? Label = null);

/// <summary>A normalized tile key in the slippy-map tile scheme.</summary>
public readonly record struct MapTileKey
{
    /// <summary>Creates and normalizes a tile key.</summary>
    public MapTileKey(int zoom, int x, int y)
    {
        if (zoom is < 0 or > 22)
            throw new ArgumentOutOfRangeException(nameof(zoom), zoom, "Tile zoom must be between 0 and 22.");

        var tileCount = 1 << zoom;
        if (y is < 0 || y >= tileCount)
            throw new ArgumentOutOfRangeException(nameof(y), y, "Tile Y must be within the world.");

        Zoom = zoom;
        X = Modulo(x, tileCount);
        Y = y;
    }

    /// <summary>Tile zoom.</summary>
    public int Zoom { get; }

    /// <summary>Normalized tile X.</summary>
    public int X { get; }

    /// <summary>Tile Y.</summary>
    public int Y { get; }

    static int Modulo(int value, int modulus)
    {
        var remainder = value % modulus;
        return remainder < 0 ? remainder + modulus : remainder;
    }
}

/// <summary>A decoded raster tile supplied by an application-level provider.</summary>
public sealed record MapTile(MapTileKey Key, Bitmap Image);

/// <summary>Loads map tiles without coupling the control to a network provider.</summary>
public interface IMapTileSource
{
    /// <summary>Loads a tile, returning <see langword="null" /> when it is unavailable.</summary>
    ValueTask<MapTile?> GetTileAsync(
        MapTileKey key,
        CancellationToken cancellationToken = default);
}
