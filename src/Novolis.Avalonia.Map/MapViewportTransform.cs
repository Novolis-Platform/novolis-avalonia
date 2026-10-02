using Avalonia;
using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Map;

/// <summary>Transforms geographic coordinates and map tiles for a viewport.</summary>
public readonly struct MapViewportTransform
{
    /// <summary>Standard raster tile edge length in pixels.</summary>
    public const double TileSizePixels = 256;

    readonly GeoProjectedPoint _centerProjected;
    readonly double _worldPixels;

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
        _centerProjected = WebMercatorProjection.Project(viewport.Center);
        _worldPixels = TileSizePixels * global::System.Math.Pow(2, viewport.Zoom);
    }

    /// <summary>Viewport represented by this transform.</summary>
    public MapViewport Viewport { get; }

    /// <summary>Viewport width in pixels.</summary>
    public double Width { get; }

    /// <summary>Viewport height in pixels.</summary>
    public double Height { get; }

    /// <summary>Pixel width of one wrapped world at the current zoom.</summary>
    public double WorldPixels => _worldPixels;

    /// <summary>Converts a geographic coordinate to screen pixels.</summary>
    public Point GeoToScreen(GeoCoordinate coordinate)
    {
        var projected = WebMercatorProjection.Project(coordinate);
        var deltaX = projected.X - _centerProjected.X;
        if (deltaX > 0.5)
            deltaX -= 1;
        else if (deltaX < -0.5)
            deltaX += 1;

        return new Point(
            Width / 2 + deltaX * _worldPixels,
            Height / 2 + (projected.Y - _centerProjected.Y) * _worldPixels);
    }

    /// <summary>Converts screen pixels into a geographic coordinate.</summary>
    public GeoCoordinate ScreenToGeo(Point point)
    {
        var projectedX = WrapNormalized(
            _centerProjected.X + (point.X - Width / 2) / _worldPixels);
        var projectedY = global::System.Math.Clamp(
            _centerProjected.Y + (point.Y - Height / 2) / _worldPixels,
            0d,
            1d);

        return WebMercatorProjection.Unproject(new GeoProjectedPoint(projectedX, projectedY));
    }

    /// <summary>Returns the tile keys that intersect this viewport.</summary>
    public IReadOnlyList<MapTileKey> GetVisibleTileKeys()
    {
        var tileZoom = global::System.Math.Clamp(
            (int)global::System.Math.Round(Viewport.Zoom),
            0,
            22);
        var tileCount = 1 << tileZoom;
        var tileSize = TileSizePixels
            * global::System.Math.Pow(2, Viewport.Zoom - tileZoom);
        var centerWorldX = _centerProjected.X * tileCount * tileSize;
        var centerWorldY = _centerProjected.Y * tileCount * tileSize;
        var left = centerWorldX - Width / 2;
        var right = centerWorldX + Width / 2;
        var top = centerWorldY - Height / 2;
        var bottom = centerWorldY + Height / 2;
        var firstX = (int)global::System.Math.Floor(left / tileSize);
        var lastX = (int)global::System.Math.Floor((right - double.Epsilon) / tileSize);
        var firstY = (int)global::System.Math.Floor(top / tileSize);
        var lastY = (int)global::System.Math.Floor((bottom - double.Epsilon) / tileSize);
        var keys = new HashSet<MapTileKey>();

        for (var x = firstX; x <= lastX; x++)
        {
            for (var y = firstY; y <= lastY; y++)
            {
                if (y >= 0 && y < tileCount)
                    keys.Add(new MapTileKey(tileZoom, x, y));
            }
        }

        return keys.ToArray();
    }

    /// <summary>Returns the screen rectangle for a tile in this viewport.</summary>
    public Rect TileToScreenRect(MapTileKey key)
    {
        var tileCount = 1 << key.Zoom;
        var tileSize = TileSizePixels
            * global::System.Math.Pow(2, Viewport.Zoom - key.Zoom);
        var worldSize = tileCount * tileSize;
        var centerWorldX = _centerProjected.X * worldSize;
        var centerWorldY = _centerProjected.Y * worldSize;
        var tileWorldX = key.X * tileSize;
        var deltaX = tileWorldX - centerWorldX;

        while (deltaX > worldSize / 2)
            deltaX -= worldSize;
        while (deltaX < -worldSize / 2)
            deltaX += worldSize;

        return new Rect(
            Width / 2 + deltaX,
            Height / 2 + key.Y * tileSize - centerWorldY,
            tileSize,
            tileSize);
    }

    /// <summary>Calculates a new center that keeps an anchor coordinate under a screen point.</summary>
    public static GeoCoordinate CenterForAnchor(
        MapViewport viewport,
        double width,
        double height,
        GeoCoordinate anchor,
        Point screen)
    {
        var projectedAnchor = WebMercatorProjection.Project(anchor);
        var worldPixels = TileSizePixels * global::System.Math.Pow(2, viewport.Zoom);
        var centerX = WrapNormalized(
            projectedAnchor.X - (screen.X - width / 2) / worldPixels);
        var centerY = global::System.Math.Clamp(
            projectedAnchor.Y - (screen.Y - height / 2) / worldPixels,
            0d,
            1d);

        return WebMercatorProjection.Unproject(new GeoProjectedPoint(centerX, centerY));
    }

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

    static double WrapNormalized(double value)
    {
        var wrapped = value % 1d;
        return wrapped < 0 ? wrapped + 1d : wrapped;
    }
}
