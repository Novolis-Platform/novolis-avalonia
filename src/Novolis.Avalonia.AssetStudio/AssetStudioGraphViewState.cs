using System.Collections.Immutable;
using Avalonia;
using Novolis.Visual.Definitions;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Persistent graph-only view state. It is deliberately separate from the
/// semantic recipe, so navigation never changes a module fingerprint.
/// </summary>
public sealed record AssetStudioGraphViewState(
    double Zoom,
    Vector Pan,
    string? SelectedOutputId,
    ImmutableDictionary<NodeId, Point> NodePositions)
{
    public static AssetStudioGraphViewState Default { get; } =
        new(
            1d,
            Vector.Zero,
            null,
            ImmutableDictionary<NodeId, Point>.Empty);
}
