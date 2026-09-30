using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Novolis.Avalonia.StarMap;

/// <summary>A route edge between two point ids.</summary>
public sealed class StarMapEdge
{
    /// <summary>From id.</summary>
    public required string FromId { get; init; }

    /// <summary>To id.</summary>
    public required string ToId { get; init; }

    /// <summary>Optional stroke band (e.g. lane / corridor class).</summary>
    public string? BandTag { get; init; }
}
