namespace Novolis.Avalonia.Controls;

/// <summary>A freehand stroke committed as a first-class resizable shape.</summary>
public sealed class StrokeShape
{
    /// <summary>Stable element id.</summary>
    public required string Id { get; init; }

    /// <summary>Polyline points in world space.</summary>
    public List<SketchPoint> Points { get; set; } = [];

    /// <summary>Stroke color as #RRGGBB or #AARRGGBB.</summary>
    public string StrokeColor { get; set; } = "#1e1e1e";

    /// <summary>Stroke width in world units (hairlines down to ~0.25 supported).</summary>
    public double StrokeWidth { get; set; } = 2;

    /// <summary>Optional fill color (#RRGGBB). Null/empty = unfilled.</summary>
    public string? FillColor { get; set; }

    /// <summary>Dash / stipple pattern.</summary>
    public SketchStrokeStyle StrokeStyle { get; set; } = SketchStrokeStyle.Solid;

    /// <summary>When true, path is treated as a closed polygon (fill + join ends).</summary>
    public bool Closed { get; set; }

    /// <summary>Deep-clones this stroke.</summary>
    public StrokeShape Clone() => new()
    {
        Id = Id,
        Points = [.. Points],
        StrokeColor = StrokeColor,
        StrokeWidth = StrokeWidth,
        FillColor = FillColor,
        StrokeStyle = StrokeStyle,
        Closed = Closed
    };
}
