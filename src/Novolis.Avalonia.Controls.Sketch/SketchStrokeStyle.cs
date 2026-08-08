using Avalonia.Media.Immutable;

namespace Novolis.Avalonia.Controls.Sketch;

/// <summary>Stroke dash pattern for sketch polylines.</summary>
public enum SketchStrokeStyle
{
    /// <summary>Continuous stroke.</summary>
    Solid = 0,

    /// <summary>Long dashes.</summary>
    Dashed = 1,

    /// <summary>Round-capped dots.</summary>
    Dotted = 2,

    /// <summary>Dash–dot rhythm.</summary>
    DashDot = 3,

    /// <summary>Dense stipple (short gaps).</summary>
    Stipple = 4
}

/// <summary>Dash pattern helpers for canvas pens and SVG export.</summary>
public static class SketchStrokeStyles
{
    /// <summary>Avalonia dash style scaled by stroke thickness, or null for solid.</summary>
    public static ImmutableDashStyle? CreateDash(SketchStrokeStyle style, double thickness)
    {
        var t = Math.Max(0.35, thickness);
        return style switch
        {
            SketchStrokeStyle.Dashed => new ImmutableDashStyle([4.5 * t, 3 * t], 0),
            SketchStrokeStyle.Dotted => new ImmutableDashStyle([0.15 * t, 2.4 * t], 0),
            SketchStrokeStyle.DashDot => new ImmutableDashStyle([5.5 * t, 2.2 * t, 0.15 * t, 2.2 * t], 0),
            SketchStrokeStyle.Stipple => new ImmutableDashStyle([0.2 * t, 1.15 * t], 0),
            _ => null
        };
    }

    /// <summary>SVG <c>stroke-dasharray</c> value, or null for solid.</summary>
    public static string? SvgDashArray(SketchStrokeStyle style, double width)
    {
        var t = Math.Max(0.35, width);
        return style switch
        {
            SketchStrokeStyle.Dashed => FormattableString.Invariant($"{4.5 * t:0.###} {3 * t:0.###}"),
            SketchStrokeStyle.Dotted => FormattableString.Invariant($"{0.15 * t:0.###} {2.4 * t:0.###}"),
            SketchStrokeStyle.DashDot => FormattableString.Invariant(
                $"{5.5 * t:0.###} {2.2 * t:0.###} {0.15 * t:0.###} {2.2 * t:0.###}"),
            SketchStrokeStyle.Stipple => FormattableString.Invariant($"{0.2 * t:0.###} {1.15 * t:0.###}"),
            _ => null
        };
    }
}
