using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Novolis.Avalonia.StarMap;

/// <summary>A plotted star on the map (map units, typically light-years on XZ).</summary>
public sealed class StarMapPoint
{
    /// <summary>Stable id.</summary>
    public required string Id { get; init; }

    /// <summary>Display label.</summary>
    public string? Label { get; init; }

    /// <summary>X map coordinate.</summary>
    public double X { get; init; }

    /// <summary>Y map coordinate (often catalog Z).</summary>
    public double Y { get; init; }

    /// <summary>Optional draw radius override (screen px); null uses defaults.</summary>
    public double? Radius { get; init; }
}
