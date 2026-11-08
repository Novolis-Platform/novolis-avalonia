using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Scene;
using Novolis.Cad.Hierarchy;
using Novolis.Cad.Primitives;
using Novolis.Cad.Visibility;

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
            _tree.ItemsSource = _session.Hierarchy is { } hierarchy
                ? BuildHierarchy(hierarchy)
                : CadSceneGraph.BuildTree(_session.Document, _workspace).Select(ToItem).ToList();
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

    private List<TreeViewItem> BuildHierarchy(CadHierarchyRegistry hierarchy)
    {
        var roots = new List<TreeViewItem>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rootId = AssemblyId.Of(hierarchy.RootId);
        if (hierarchy.TryGetAssembly(rootId, out var root))
        {
            roots.Add(ToHierarchyAssembly(root, hierarchy, visited));
            return roots;
        }

        foreach (var assembly in hierarchy.Assemblies
                     .Where(a => a.ParentAssemblyId is null
                                 || string.Equals(
                                     a.ParentAssemblyId.Value.Value,
                                     hierarchy.RootId,
                                     StringComparison.OrdinalIgnoreCase))
                     .OrderBy(a => a.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            roots.Add(ToHierarchyAssembly(assembly, hierarchy, visited));
        }

        foreach (var part in hierarchy.PartsOf(rootId)
                     .OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            roots.Add(ToHierarchyPart(part));
        }

        foreach (var assembly in hierarchy.Assemblies
                     .Where(a => !visited.Contains(a.Id.Value))
                     .OrderBy(a => a.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            roots.Add(ToHierarchyAssembly(assembly, hierarchy, visited));
        }

        return roots;
    }

    private TreeViewItem ToHierarchyAssembly(
        CadHierarchyAssembly assembly,
        CadHierarchyRegistry hierarchy,
        ISet<string> visited)
    {
        var item = new TreeViewItem
        {
            Header = HierarchyHeader(
                "▢",
                DisplayName(assembly.DisplayName, assembly.Id.Value),
                assembly.Id.Value),
            IsExpanded = true,
        };
        if (!visited.Add(assembly.Id.Value))
            return item;

        foreach (var child in hierarchy.ChildAssemblies(assembly.Id)
                     .OrderBy(a => a.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            item.Items.Add(ToHierarchyAssembly(child, hierarchy, visited));
        }

        foreach (var part in hierarchy.PartsOf(assembly.Id)
                     .OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            item.Items.Add(ToHierarchyPart(part));
        }

        return item;
    }

    private TreeViewItem ToHierarchyPart(CadHierarchyPart part) =>
        new()
        {
            Header = HierarchyHeader(
                "◆",
                DisplayName(part.DisplayName, part.Id.Value),
                part.Id.Value),
            Tag = part.EntityId,
        };

    private Control HierarchyHeader(string glyph, string name, string nodeId)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.Children.Add(new TextBlock
        {
            Text = $"{glyph} {name}",
            VerticalAlignment = VerticalAlignment.Center,
        });

        var local = new ComboBox
        {
            ItemsSource = new[]
            {
                LocalVisibility.Inherit,
                LocalVisibility.Visible,
                LocalVisibility.Hidden,
            },
            SelectedItem = _session.Visibility.GetLocal(nodeId),
            MinWidth = 76,
        };
        local.SelectionChanged += (_, _) =>
        {
            if (local.SelectedItem is LocalVisibility state)
                _session.Visibility.SetLocal(nodeId, state);
        };
        row.Children.Add(local);

        var isolate = new Button
        {
            Content = _session.Visibility.IsolationActive ? "End isolate" : "Isolate",
            Padding = new global::Avalonia.Thickness(6, 2),
        };
        isolate.Click += (_, _) =>
        {
            if (_session.Visibility.IsolationActive)
                _session.Visibility.EndIsolation();
            else
                _session.Visibility.BeginIsolation(nodeId);
        };
        row.Children.Add(isolate);
        return row;
    }

    private static string DisplayName(string? name, string fallback) =>
        string.IsNullOrWhiteSpace(name) ? fallback : name;

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
