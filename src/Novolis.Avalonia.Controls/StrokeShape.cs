namespace Novolis.Avalonia.Controls;

/// <summary>A committed sketch element (stroke, text, text box, or image).</summary>
public sealed class StrokeShape
{
    /// <summary>Stable element id.</summary>
    public required string Id { get; init; }

    /// <summary>Element kind (default stroke).</summary>
    public SketchElementKind Kind { get; set; } = SketchElementKind.Stroke;

    /// <summary>Polyline / placement points in world space (local, before rotation).</summary>
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

    /// <summary>Rotation in degrees around the local AABB center.</summary>
    public double RotationDegrees { get; set; }

    /// <summary>Optional group id for fused multi-select units.</summary>
    public string? GroupId { get; set; }

    /// <summary>Text content for <see cref="SketchElementKind.Text"/> / <see cref="SketchElementKind.TextBox"/>.</summary>
    public string? Text { get; set; }

    /// <summary>Font size in world units for text elements.</summary>
    public double FontSize { get; set; } = 16;

    /// <summary>PNG payload (base64) for <see cref="SketchElementKind.Image"/>.</summary>
    public string? ImagePngBase64 { get; set; }

    /// <summary>Deep-clones this element.</summary>
    public StrokeShape Clone() => new()
    {
        Id = Id,
        Kind = Kind,
        Points = [.. Points],
        StrokeColor = StrokeColor,
        StrokeWidth = StrokeWidth,
        FillColor = FillColor,
        StrokeStyle = StrokeStyle,
        Closed = Closed,
        RotationDegrees = RotationDegrees,
        GroupId = GroupId,
        Text = Text,
        FontSize = FontSize,
        ImagePngBase64 = ImagePngBase64
    };
}
