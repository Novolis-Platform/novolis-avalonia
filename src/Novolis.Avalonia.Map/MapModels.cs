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
