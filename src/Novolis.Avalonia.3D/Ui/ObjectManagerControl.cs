using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Novolis.Agent.Surface;
using Novolis.Avalonia._3D.Session;
using Novolis.Modeling.Scene;

namespace Novolis.Avalonia._3D.Ui;

/// <summary>Scene hierarchy list — always shows selectable nodes (Root + mesh/lights/cameras).</summary>
public sealed class ObjectManagerControl : UserControl
{
    private readonly SceneSessionService _session;
    private readonly TreeView _tree = new()
    {
        SelectionMode = SelectionMode.Single,
        Foreground = Brushes.WhiteSmoke,
    };
    private bool _suppress;

    public ObjectManagerControl(SceneSessionService session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _tree.SelectionChanged += OnSelectionChanged;
        Content = new DockPanel
        {
            Background = new SolidColorBrush(Color.FromRgb(18, 26, 34)),
            Children =
            {
                new TextBlock
                {
                    Text = "Scene",
                    FontWeight = FontWeight.SemiBold,
                    Foreground = Brushes.WhiteSmoke,
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
        _suppress = true;
        try
        {
            var roots = _session.Document.Roots().ToList();
            // Orphan recovery: if parenting failed on load, still show every node as selectable.
            if (roots.Count == 0 && _session.Document.Nodes.Count > 0)
                roots = _session.Document.Nodes.ToList();

            var items = roots.Select(BuildItem).ToList();
            // Always expose the primary mesh at the top level if somehow not under Root.
            EnsureMeshVisible(items);

            _tree.ItemsSource = items;

            if (_session.Document.SelectionId is { } sel)
                SelectInTree(sel);
        }
        finally
        {
            _suppress = false;
        }
    }

    private void EnsureMeshVisible(List<TreeViewItem> roots)
    {
        var meshes = _session.Document.Nodes.OfType<MeshNode>().ToList();
        if (meshes.Count == 0)
            return;

        foreach (var mesh in meshes)
        {
            if (ContainsTag(roots, mesh.Id))
                continue;
            // Promote orphan mesh into the tree so the ship is always selectable.
            roots.Add(BuildItem(mesh));
        }
    }

    private static bool ContainsTag(IEnumerable<object?> items, Guid id)
    {
        foreach (var obj in items)
        {
            if (obj is not TreeViewItem item)
                continue;
            if (item.Tag is Guid g && g == id)
                return true;
            if (ContainsTag(item.Items.Cast<object?>(), id))
                return true;
        }

        return false;
    }

    private TreeViewItem BuildItem(SceneNode node)
    {
        var item = new TreeViewItem
        {
            Header = $"{Icon(node)} {node.Name}",
            Tag = node.Id,
            IsExpanded = true,
            Foreground = Brushes.WhiteSmoke,
        };
        foreach (var child in _session.Document.ChildrenOf(node.Id))
            item.Items.Add(BuildItem(child));
        return item;
    }

    private static string Icon(SceneNode node) => node switch
    {
        LightNode => "☀",
        CameraNode => "◎",
        MeshNode m => m.Vertices is { Length: > 0 } ? "◆" : m.Primitive switch
        {
            MeshPrimitiveKind.Sphere or MeshPrimitiveKind.Disc => "●",
            MeshPrimitiveKind.Landscape => "≋",
            _ => "◼",
        },
        MaterialNode => "◐",
        GeneratorNode g => g.Generator == GeneratorKind.Boole ? "⊖" : "⧉",
        ModifierNode => "✎",
        GroupNode => "▢",
        _ => "○",
    };

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppress)
            return;
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
        if (_tree.ItemsSource is not System.Collections.IEnumerable source)
            return;
        foreach (var obj in source)
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
