using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.GraphicalProfile;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using ProfileBinding = Novolis.Avalonia.GraphicalProfile.GraphicalProfileBinding;
using Novolis.Visual.Definitions;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Keyboard-first ordered projection of nodes and their ports.
/// </summary>
public sealed class AssetStudioNodePortList : ListBox
{
    private IReadOnlyList<AssetStudioNodePortEntry> _entries = [];

    public AssetStudioNodePortList()
    {
        SelectionMode = SelectionMode.Single;
        MinHeight = 160;
        SetValue(
            AutomationProperties.NameProperty,
            "Ordered node and port workflow");
        SetValue(
            AutomationProperties.AutomationIdProperty,
            "assetstudio.graph.nodePorts");

        ItemTemplate = new FuncDataTemplate<AssetStudioNodePortEntry>(
            (entry, _) =>
            {
                var row = new TextBlock
                {
                    Text = entry.DisplayText,
                    Margin = new Thickness(entry.IsPort ? 24 : 6, 4, 6, 4),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                row.SetValue(AutomationProperties.NameProperty, entry.AutomationName);
                ProfileBinding.Bind(
                    row,
                    TextBlock.ForegroundProperty,
                    Profile.TextResourceKey);
                return row;
            },
            supportsRecycling: true);

        KeyDown += OnKeyDown;
    }

    /// <summary>Rows currently projected from the semantic recipe.</summary>
    public IReadOnlyList<AssetStudioNodePortEntry> Entries => _entries;

    /// <summary>Currently selected node or port row.</summary>
    public AssetStudioNodePortEntry? SelectedEntry =>
        SelectedItem as AssetStudioNodePortEntry;

    /// <summary>Raised when the selected row is activated with Enter or Space.</summary>
    public event EventHandler? EntryInvoked;

    /// <summary>Raised for keyboard node actions such as delete and duplicate.</summary>
    public event EventHandler<AssetStudioGraphNodeEventArgs>?
        NodeActionRequested;

    /// <summary>
    /// Projects a recipe into stable node-first, port-second rows.
    /// </summary>
    public void SetRecipe(
        VisualRecipe recipe,
        VisualNodeRegistry? registry = null)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        registry ??= VisualNodeRegistry.CreateDefault();

        var entries = new List<AssetStudioNodePortEntry>();
        foreach (var node in recipe.Nodes.OrderBy(node => node.Id))
        {
            registry.TryGetDescriptor(node.TypeId, out var descriptor);
            var nodeLabel = descriptor?.DisplayName ?? $"{node.TypeId} (unknown)";
            entries.Add(new AssetStudioNodePortEntry(
                node.Id,
                nodeLabel,
                node.TypeId));

            if (descriptor is null)
            {
                continue;
            }

            foreach (var port in descriptor.Ports.OrderBy(
                         port => port.Id.Value,
                         StringComparer.Ordinal))
            {
                entries.Add(new AssetStudioNodePortEntry(
                    node.Id,
                    nodeLabel,
                    port.Id.Value,
                    port.Id,
                    port.Direction,
                    port.Contract));
            }
        }

        _entries = entries;
        ItemsSource = _entries;
        SelectedIndex = _entries.Count == 0 ? -1 : 0;
    }

    /// <summary>Moves the selected row by a relative ordered-list offset.</summary>
    public void MoveSelection(int offset)
    {
        if (_entries.Count == 0)
        {
            SelectedIndex = -1;
            return;
        }

        var next = global::System.Math.Clamp(
            SelectedIndex < 0 ? 0 : SelectedIndex + offset,
            0,
            _entries.Count - 1);
        SelectedIndex = next;
        Focus();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (SelectedEntry is { IsPort: false } entry &&
            e.Key == Key.Delete)
        {
            NodeActionRequested?.Invoke(
                this,
                new AssetStudioGraphNodeEventArgs(
                    entry.NodeId,
                    "delete"));
            e.Handled = true;
            return;
        }

        if (SelectedEntry is { IsPort: false } duplicate &&
            e.Key == Key.D &&
            (e.KeyModifiers & KeyModifiers.Control) != 0)
        {
            NodeActionRequested?.Invoke(
                this,
                new AssetStudioGraphNodeEventArgs(
                    duplicate.NodeId,
                    "duplicate"));
            e.Handled = true;
            return;
        }

        if (e.Key is Key.Enter or Key.Space)
        {
            EntryInvoked?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }
}
