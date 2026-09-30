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
