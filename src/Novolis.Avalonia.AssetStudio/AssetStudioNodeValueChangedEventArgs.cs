using Novolis.Visual.Definitions;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>Reports one descriptor-defined node value edit.</summary>
public sealed class AssetStudioNodeValueChangedEventArgs : EventArgs
{
    public AssetStudioNodeValueChangedEventArgs(
        NodeId nodeId,
        string valueName,
        VisualValue value)
    {
        NodeId = nodeId;
        ValueName = string.IsNullOrWhiteSpace(valueName)
            ? throw new ArgumentException(
                "A value name is required.",
                nameof(valueName))
            : valueName;
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public NodeId NodeId { get; }

    public string ValueName { get; }

    public VisualValue Value { get; }
}
