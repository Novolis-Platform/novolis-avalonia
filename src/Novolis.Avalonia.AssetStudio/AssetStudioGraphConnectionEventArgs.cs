using Novolis.Visual.Definitions;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>Requests a typed graph edge from the shared authoring session.</summary>
public sealed class AssetStudioGraphConnectionEventArgs : EventArgs
{
    public AssetStudioGraphConnectionEventArgs(
        VisualPortReference source,
        VisualPortReference target)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }

    public VisualPortReference Source { get; }

    public VisualPortReference Target { get; }
}
