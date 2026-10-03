using Avalonia;
using Novolis.IO.Maps;
using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Map;

/// <summary>Transforms geographic coordinates and map tiles for a viewport.</summary>
public readonly struct MapViewportTransform
{
    /// <summary>Standard raster tile edge length in pixels.</summary>
    public const double TileSizePixels = WebMercatorTiles.TileSizePixels;

    /// <summary>Creates a transform for the given viewport size.</summary>
    public MapViewportTransform(MapViewport viewport, double width, double height)
    {
        if (!double.IsFinite(width) || width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be finite and greater than zero.");

        if (!double.IsFinite(height) || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be finite and greater than zero.");

        Viewport = viewport;
        Width = width;
        Height = height;
    }

    /// <summary>Viewport represented by this transform.</summary>
    public MapViewport Viewport { get; }

    /// <summary>Viewport width in pixels.</summary>
    public double Width { get; }

    /// <summary>Viewport height in pixels.</summary>
    public double Height { get; }

    /// <summary>Pixel width of one wrapped world at the current zoom.</summary>
    public double WorldPixels =>
        TileSizePixels * global::System.Math.Pow(2, Viewport.Zoom);

    /// <summary>Converts a geographic coordinate to screen pixels.</summary>
    public Point GeoToScreen(GeoCoordinate coordinate)
    {
        var pixel = WebMercatorTiles.GeoToPixel(
            Viewport.Center,
            Viewport.Zoom,
            Width,
            Height,
            coordinate);
        return new Point(pixel.X, pixel.Y);
    }

    /// <summary>Converts screen pixels into a geographic coordinate.</summary>
    public GeoCoordinate ScreenToGeo(Point point)
        => WebMercatorTiles.PixelToGeo(
            Viewport.Center,
            Viewport.Zoom,
            Width,
            Height,
            point.X,
            point.Y);

    /// <summary>Returns the tile keys that intersect this viewport.</summary>
    public IReadOnlyList<MapTileKey> GetVisibleTileKeys()
        => WebMercatorTiles.VisibleTiles(
            Viewport.Center,
            Viewport.Zoom,
            Width,
            Height);

    /// <summary>Returns the screen rectangle for a tile in this viewport.</summary>
    public Rect TileToScreenRect(MapTileKey key)
    {
        var rectangle = WebMercatorTiles.TilePixelRect(
            Viewport.Center,
            Viewport.Zoom,
            Width,
            Height,
            key);
        return new Rect(
            rectangle.X,
            rectangle.Y,
            rectangle.Width,
            rectangle.Height);
    }

    /// <summary>Calculates a new center that keeps an anchor coordinate under a screen point.</summary>
    public static GeoCoordinate CenterForAnchor(
        MapViewport viewport,
        double width,
        double height,
        GeoCoordinate anchor,
        Point screen)
        => WebMercatorTiles.CenterForAnchor(
            viewport.Zoom,
            width,
            height,
            anchor,
            screen.X,
            screen.Y);

    /// <summary>Moves the viewport by a screen-pixel delta. Positive X follows a finger moving right.</summary>
    public MapViewport Translate(Vector screenDelta)
    {
        var center = ScreenToGeo(new Point(
            Width / 2 - screenDelta.X,
            Height / 2 - screenDelta.Y));
        return new MapViewport(center, Viewport.Zoom);
    }

    /// <summary>
    /// Zooms from a gesture start so the anchor stays under the current pinch midpoint.
    /// Distance is in screen pixels. Doubling the distance adds one zoom level.
    /// </summary>
    public static MapViewport Pinch(
        MapViewport start,
        double width,
        double height,
        GeoCoordinate anchor,
        Point midpoint,
        double startDistance,
        double distance)
    {
        var safeStart = global::System.Math.Max(1, startDistance);
        var safeDistance = global::System.Math.Max(1, distance);
        var zoom = global::System.Math.Clamp(
            start.Zoom + global::System.Math.Log(safeDistance / safeStart, 2),
            MapViewport.MinimumZoom,
            MapViewport.MaximumZoom);
        var zoomed = new MapViewport(start.Center, zoom);
        var center = CenterForAnchor(zoomed, width, height, anchor, midpoint);
        return new MapViewport(center, zoom);
    }
}
