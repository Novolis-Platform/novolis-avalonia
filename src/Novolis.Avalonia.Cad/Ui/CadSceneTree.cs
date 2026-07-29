using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Scene;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Cad.Ui;

/// <summary>Shared scene hierarchy tree for all workspaces.</summary>
public sealed class CadSceneTree : UserControl
{
    private readonly CadDocumentSession _session;
    private readonly TreeView _tree = new()
    {
        SelectionMode = SelectionMode.Single,
    };
    private CadWorkspace _workspace = CadWorkspace.Cad;
    private bool _suppress;

    public CadSceneTree(CadDocumentSession session)
    {
        _session = session;
        Content = _tree;
        _tree.SelectionChanged += OnSelectionChanged;
        _session.Changed += Refresh;
        Refresh();
    }

    public CadWorkspace Workspace
    {
        get => _workspace;
        set
        {
            if (_workspace == value)
                return;
            _workspace = value;
            Refresh();
        }
    }

    public event Action<Guid?>? EntitySelected;

    public void Refresh()
    {
        _suppress = true;
        try
        {
            var roots = CadSceneGraph.BuildTree(_session.Document, _workspace);
            _tree.ItemsSource = roots.Select(ToItem).ToList();
            if (_session.SelectedId is { } sid)
                SelectById(sid);
        }
        finally
        {
            _suppress = false;
        }
    }

    private TreeViewItem ToItem(CadSceneTreeNode node)
    {
        var item = new TreeViewItem
        {
            Header = $"{CategoryGlyph(node.Category)} {node.Name}",
            Tag = node.Id,
            IsExpanded = true,
        };
        foreach (var child in node.Children)
            item.Items.Add(ToItem(child));
        return item;
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppress)
            return;
        if (_tree.SelectedItem is TreeViewItem { Tag: Guid id })
        {
            _session.SelectedId = id;
            EntitySelected?.Invoke(id);
            _session.Notify();
        }
    }

    private void SelectById(Guid id)
    {
        foreach (var obj in _tree.Items)
        {
            if (obj is TreeViewItem item && Find(item, id) is { } hit)
            {
                hit.IsSelected = true;
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
            if (child is TreeViewItem ti && Find(ti, id) is { } hit)
                return hit;
        }

        return null;
    }

    private static string CategoryGlyph(CadSceneNodeCategory c) => c switch
    {
        CadSceneNodeCategory.Group => "▢",
        CadSceneNodeCategory.Generator => "◈",
        CadSceneNodeCategory.MeshFromSolid => "▦",
        CadSceneNodeCategory.MeshModifier => "↻",
        CadSceneNodeCategory.Material => "◐",
        CadSceneNodeCategory.Light => "☀",
        CadSceneNodeCategory.Camera => "◉",
        CadSceneNodeCategory.Geometry => "◆",
        _ => "•",
    };
}
