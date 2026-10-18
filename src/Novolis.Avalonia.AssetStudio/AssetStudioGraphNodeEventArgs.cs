using Novolis.Visual.Definitions;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>Selection or command context from the graph workspace.</summary>
public sealed class AssetStudioGraphNodeEventArgs : EventArgs
{
    public AssetStudioGraphNodeEventArgs(
        NodeId nodeId,
        string action = "select")
    {
        NodeId = nodeId;
        Action = action;
    }

    public NodeId NodeId { get; }

    public string Action { get; }
}
