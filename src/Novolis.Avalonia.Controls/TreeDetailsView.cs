using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Novolis.Avalonia.Layout;

namespace Novolis.Avalonia.Controls;

/// <summary>Expandable tree for structured packet or object details.</summary>
public sealed class TreeDetailsView : ScrollViewer
{
    private readonly TreeView _tree;

    public TreeDetailsView()
    {
        _tree = new TreeView();
        Content = _tree;
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
    }

    public void SetRoot(DetailTreeNode? root)
    {
        _tree.Items.Clear();
        if (root is null)
            return;

        _tree.Items.Add(CreateItem(root));
    }

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
