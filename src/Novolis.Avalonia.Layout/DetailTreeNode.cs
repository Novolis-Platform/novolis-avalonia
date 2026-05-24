namespace Novolis.Avalonia.Layout;

/// <summary>UI-agnostic tree node for protocol or structured detail panes.</summary>
public sealed class DetailTreeNode
{
    /// <summary>Creates a detail node with optional description and child nodes.</summary>
    /// <param name="title">Primary label shown in the tree.</param>
    /// <param name="description">Optional secondary text appended to the title.</param>
    /// <param name="children">Child nodes; defaults to empty when <see langword="null"/>.</param>
    public DetailTreeNode(string title, string? description = null, IReadOnlyList<DetailTreeNode>? children = null)
    {
        Title = title;
        Description = description;
        Children = children ?? [];
    }

    /// <summary>Primary label for this node.</summary>
    public string Title { get; }

    /// <summary>Optional detail text displayed after the title.</summary>
    public string? Description { get; }

    /// <summary>Child nodes in display order.</summary>
    public IReadOnlyList<DetailTreeNode> Children { get; }
}
