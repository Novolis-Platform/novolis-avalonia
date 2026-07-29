using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Agent.Surface;
using Novolis.Avalonia._3D.Session;
using Novolis.Modeling.Scene;

namespace Novolis.Avalonia._3D.Ui;

/// <summary>Object Manager hierarchy list.</summary>
public sealed class ObjectManagerControl : UserControl
{
    private readonly SceneSessionService _session;
    private readonly TreeView _tree = new();

    public ObjectManagerControl(SceneSessionService session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _tree.SelectionChanged += OnSelectionChanged;
        Content = new DockPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = "Object Manager",
                    FontWeight = FontWeight.SemiBold,
                    Margin = new Thickness(8, 8, 8, 4),
                    [DockPanel.DockProperty] = Dock.Top,
                },
                _tree,
            },
        };
        _session.DocumentChanged += Refresh;
        Refresh();
    }

    public void Refresh()
    {
        _tree.Items.Clear();
        foreach (var root in _session.Document.Roots())
            _tree.Items.Add(BuildItem(root));

        if (_session.Document.SelectionId is { } sel)
            SelectInTree(sel);
    }

    private TreeViewItem BuildItem(SceneNode node)
    {
        var item = new TreeViewItem
        {
            Header = $"{Icon(node)} {node.Name}",
            Tag = node.Id,
            IsExpanded = true,
        };
        foreach (var child in _session.Document.ChildrenOf(node.Id))
            item.Items.Add(BuildItem(child));
        return item;
    }

    private static string Icon(SceneNode node) => node switch
    {
        LightNode => "☀",
        CameraNode => "◎",
        MeshNode => "◼",
        MaterialNode => "◐",
        GeneratorNode => "⧉",
        ModifierNode => "✎",
        GroupNode => "▢",
        _ => "○",
    };

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_tree.SelectedItem is TreeViewItem { Tag: Guid id })
        {
            _session.Execute(new AgentCommandDto
            {
                ActionId = SceneSessionActionIds.Select,
                NodeId = id.ToString(),
            });
        }
    }

    private void SelectInTree(Guid id)
    {
        foreach (var obj in _tree.Items)
        {
            if (obj is TreeViewItem item && Find(item, id) is { } match)
            {
                match.IsSelected = true;
                return;
            }
        }
    }

    private static TreeViewItem? Find(TreeViewItem item, Guid id)
    {
        if (item.Tag is Guid g && g == id)
            return item;
        foreach (var child in item.Items)
        {
            if (child is TreeViewItem c && Find(c, id) is { } hit)
                return hit;
        }

        return null;
    }
}
