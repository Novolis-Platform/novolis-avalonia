using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using Novolis.Agent.Core;
using Novolis.Agent.Surface;
using Novolis.Avalonia._3D.Session;
using Novolis.Modeling.Scene;

namespace Novolis.Avalonia._3D.Ui;

/// <summary>Scene hierarchy — Root + mesh/lights/cameras via TreeDataTemplate (not nested TreeViewItems).</summary>
public sealed class ObjectManagerControl : UserControl
{
    private readonly SceneSessionService _session;
    private readonly TreeView _tree;
    private readonly ObservableCollection<SceneTreeRow> _roots = new();
    private bool _suppress;

    public ObjectManagerControl(SceneSessionService session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));

        _tree = new TreeView
        {
            SelectionMode = SelectionMode.Single,
            Foreground = Brushes.WhiteSmoke,
            ItemsSource = _roots,
            ItemTemplate = new FuncTreeDataTemplate<SceneTreeRow>(
                (row, _) => new TextBlock
                {
                    Text = row.Header,
                    Foreground = Brushes.WhiteSmoke,
                },
                row => row.Children),
        };

        // Keep expanders open so Camera/Lights under Root are visible without hunting.
        _tree.Styles.Add(new Style(x => x.OfType<TreeViewItem>())
        {
            Setters = { new Setter(TreeViewItem.IsExpandedProperty, true) },
        });

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
            _roots.Clear();
            var roots = _session.Document.Roots().ToList();
            if (roots.Count == 0 && _session.Document.Nodes.Count > 0)
                roots = _session.Document.Nodes.ToList();

            foreach (var node in roots)
                _roots.Add(BuildRow(node));

            // Promote orphans (parent missing / broken) so nothing is invisible.
            PromoteOrphans();

            if (_session.Document.SelectionId is { } sel)
                SelectInTree(sel);
        }
        finally
        {
            _suppress = false;
        }
    }

    private void PromoteOrphans()
    {
        foreach (var node in _session.Document.Nodes)
        {
            if (ContainsId(_roots, node.Id))
                continue;
            // Skip if parent exists in doc but isn't shown yet — still show as top-level so it's selectable.
            _roots.Add(BuildRow(node));
        }
    }

    private SceneTreeRow BuildRow(SceneNode node)
    {
        var row = new SceneTreeRow($"{Icon(node)} {node.Name}", node.Id);
        foreach (var child in _session.Document.ChildrenOf(node.Id))
            row.Children.Add(BuildRow(child));
        return row;
    }

    private static bool ContainsId(IEnumerable<SceneTreeRow> rows, Guid id)
    {
        foreach (var row in rows)
        {
            if (row.Id == id)
                return true;
            if (ContainsId(row.Children, id))
                return true;
        }

        return false;
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
        if (_tree.SelectedItem is SceneTreeRow row)
        {
            _session.Execute(new AgentCommand
            {
                ActionId = SceneSessionActionIds.Select,
                NodeId = row.Id.ToString(),
            });
        }
    }

    private void SelectInTree(Guid id)
    {
        if (FindRow(_roots, id) is not { } match)
            return;
        _tree.SelectedItem = match;
    }

    private static SceneTreeRow? FindRow(IEnumerable<SceneTreeRow> rows, Guid id)
    {
        foreach (var row in rows)
        {
            if (row.Id == id)
                return row;
            if (FindRow(row.Children, id) is { } hit)
                return hit;
        }

        return null;
    }

    private sealed class SceneTreeRow(string header, Guid id)
    {
        public string Header { get; } = header;
        public Guid Id { get; } = id;
        public ObservableCollection<SceneTreeRow> Children { get; } = new();
    }
}
