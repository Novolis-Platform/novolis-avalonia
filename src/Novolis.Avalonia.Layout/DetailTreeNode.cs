namespace Novolis.Avalonia.Layout;

/// <summary>UI-agnostic tree node for protocol or structured detail panes.</summary>
public sealed class DetailTreeNode
{
    public DetailTreeNode(string title, string? description = null, IReadOnlyList<DetailTreeNode>? children = null)
    {
        Title = title;
        Description = description;
        Children = children ?? [];
    }

    public string Title { get; }

    public string? Description { get; }

    public IReadOnlyList<DetailTreeNode> Children { get; }
}
