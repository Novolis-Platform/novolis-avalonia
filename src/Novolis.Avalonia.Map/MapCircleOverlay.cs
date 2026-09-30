using Avalonia.Media.Imaging;
using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Map;

/// <summary>A geographic circle rendered by <see cref="MapControl" />.</summary>
public sealed record MapCircleOverlay(
    string Id,
    GeoCircle Circle,
    string? Label = null);
