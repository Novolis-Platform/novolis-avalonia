using Avalonia.Media;
using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Map;

/// <summary>A closed geographic polygon rendered over the map.</summary>
public sealed record MapPolygonOverlay(
    string Id,
    IReadOnlyList<GeoCoordinate> Points,
    string? Label = null,
    Color? Ink = null,
    Color? Fill = null);
