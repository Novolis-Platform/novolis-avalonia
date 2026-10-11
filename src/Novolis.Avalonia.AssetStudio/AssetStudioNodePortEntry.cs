using Novolis.Visual.Definitions;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// One deterministic row in the accessible node and port workflow.
/// </summary>
public sealed record AssetStudioNodePortEntry
{
    public AssetStudioNodePortEntry(
        NodeId nodeId,
        string nodeLabel,
        string label,
        PortId? portId = null,
        PortDirection? direction = null,
        VisualTypeContract? contract = null)
    {
        if (nodeId.IsEmpty)
        {
            throw new ArgumentException("A node identifier is required.", nameof(nodeId));
        }

        NodeLabel = string.IsNullOrWhiteSpace(nodeLabel)
            ? throw new ArgumentException("A node label is required.", nameof(nodeLabel))
            : nodeLabel;
        Label = string.IsNullOrWhiteSpace(label)
            ? throw new ArgumentException("A row label is required.", nameof(label))
            : label;

        NodeId = nodeId;
        PortId = portId;
        Direction = direction;
        Contract = contract;
    }

    /// <summary>Node instance represented by this row.</summary>
    public NodeId NodeId { get; }

    /// <summary>Human-readable node name.</summary>
    public string NodeLabel { get; }

    /// <summary>Port name, or the node type label for a node row.</summary>
    public string Label { get; }

    /// <summary>Port identifier for a port row; null for a node row.</summary>
    public PortId? PortId { get; }

    /// <summary>Port direction for a port row.</summary>
    public PortDirection? Direction { get; }

    /// <summary>Port type contract when this row represents a port.</summary>
    public VisualTypeContract? Contract { get; }

    /// <summary>True when this row represents a port rather than a node.</summary>
    public bool IsPort => PortId is not null;

    /// <summary>Stable text shown in the ordered list.</summary>
    public string DisplayText
    {
        get
        {
            if (!IsPort)
            {
                return NodeLabel;
            }

            var direction = Direction == PortDirection.Input ? "input" : "output";
            var contract = Contract is null ? string.Empty : $" · {Contract}";
            return $"{direction} · {Label}{contract}";
        }
    }

    /// <summary>Accessible name containing node and port context.</summary>
    public string AutomationName =>
        IsPort
            ? $"{NodeLabel}, {DisplayText}"
            : $"Node, {NodeLabel}";

    public override string ToString() => DisplayText;
}
