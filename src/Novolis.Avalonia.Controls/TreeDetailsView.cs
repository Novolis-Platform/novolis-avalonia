using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Novolis.Avalonia.Layout;

namespace Novolis.Avalonia.Controls;

/// <summary>Expandable tree for structured packet or object details.</summary>
public sealed class TreeDetailsView : ScrollViewer
{
    private readonly TreeView _tree;

    /// <summary>Creates an empty scrollable tree view.</summary>
    public TreeDetailsView()
    {
        _tree = new TreeView();
        Content = _tree;
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
    }

    /// <summary>Replaces the tree contents with the given root node.</summary>
    /// <param name="root">Root detail node, or <see langword="null"/> to clear.</param>
    public void SetRoot(DetailTreeNode? root)
    {
        _tree.Items.Clear();
        if (root is null)
            return;

        _tree.Items.Add(CreateItem(root));
    }

    /// <summary>Removes all tree items.</summary>
    public void Clear() => _tree.Items.Clear();

    private static TreeViewItem CreateItem(DetailTreeNode node)
    {
        var title = string.IsNullOrEmpty(node.Description)
            ? node.Title
            : $"{node.Title}: {node.Description}";
        var item = new TreeViewItem
        {
            Header = title,
            IsExpanded = true,
        };
        foreach (var child in node.Children)
            item.Items.Add(CreateItem(child));

        return item;
    }
}
