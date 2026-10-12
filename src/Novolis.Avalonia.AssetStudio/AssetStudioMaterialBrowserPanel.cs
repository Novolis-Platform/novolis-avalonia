using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Visual.Assets;
using Novolis.Visual.Definitions;
using Novolis.Visual.Materials;
using Novolis.Visual.Presets;
using Novolis.Avalonia.GraphicalProfile;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using ProfileBinding = Novolis.Avalonia.GraphicalProfile.GraphicalProfileBinding;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Searchable Material/Asset/Template/Node browser inspired by mature node
/// authoring tools.
/// </summary>
public sealed class AssetStudioMaterialBrowserPanel : AssetStudioPanel
{
    private readonly TextBox _search;
    private readonly ListBox _items;
    private VisualNodeCatalog _nodes = VisualNodeCatalog.CreateDefault();
    private MaterialCatalog _materials = new();
    private IReadOnlyList<AssetRecipe> _assets = [];
    private VisualPresetCatalog _presets = VisualPresetCatalog.CreateDefault();
    private IReadOnlyList<AssetStudioBrowserEntry> _entries = [];

    public AssetStudioMaterialBrowserPanel()
        : base(
            "Material and Asset Browser",
            CreateContent(out var search, out var items),
            "assetstudio.panel.browser")
    {
        _search = search;
        _items = items;
        _search.TextChanged += (_, _) => Refresh();
        _items.SelectionChanged += (_, _) =>
        {
            if (_items.SelectedItem is AssetStudioBrowserEntry entry)
            {
                EntrySelected?.Invoke(this, entry);
            }
        };
        Refresh();
    }

    public event EventHandler<AssetStudioBrowserEntry>? EntrySelected;

    public ListBox ItemsControl => _items;

    public IReadOnlyList<AssetStudioBrowserEntry> Entries => _entries;

    public void SetCatalogs(
        VisualNodeCatalog? nodes = null,
        MaterialCatalog? materials = null,
        VisualPresetCatalog? presets = null,
        IEnumerable<AssetRecipe>? assets = null)
    {
        _nodes = nodes ?? VisualNodeCatalog.CreateDefault();
        _materials = materials ?? new MaterialCatalog();
        _assets = (assets ?? [])
            .OrderBy(asset => asset.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(asset => asset.AssetId, StringComparer.Ordinal)
            .ToArray();
        _presets = presets ?? VisualPresetCatalog.CreateDefault();
        Refresh();
    }

    private void Refresh()
    {
        var query = _search.Text?.Trim();
        _entries = _presets.Search(query)
            .Select(
                preset =>
                    new AssetStudioBrowserEntry(
                        "Template",
                        preset.Id,
                        preset.DisplayName,
                        preset.Description))
            .Concat(
                _assets
                    .Where(
                        asset => string.IsNullOrWhiteSpace(query) ||
                                 asset.DisplayName.Contains(
                                     query!,
                                     StringComparison.OrdinalIgnoreCase) ||
                                 asset.AssetId.Contains(
                                     query!,
                                     StringComparison.OrdinalIgnoreCase) ||
                                 asset.Tags.Any(
                                     tag => tag.Contains(
                                         query!,
                                         StringComparison.OrdinalIgnoreCase)))
                    .Select(
                        asset =>
                            new AssetStudioBrowserEntry(
                                "Asset",
                                asset.AssetId,
                                asset.DisplayName,
                                string.Join(", ", asset.Tags))))
            .Concat(
                _materials.Search(query)
                    .Select(
                        material =>
                            new AssetStudioBrowserEntry(
                                "Material",
                                material.MaterialId,
                                material.DisplayName,
                                string.Join(", ", material.Tags))))
            .Concat(
                _nodes.Search(query)
                    .Select(
                        node =>
                            new AssetStudioBrowserEntry(
                                "Node",
                                node.TypeId,
                                node.DisplayName,
                                node.Editor.Description ?? node.Category.ToString())))
            .OrderBy(entry => entry.Kind, StringComparer.Ordinal)
            .ThenBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        _items.ItemsSource = _entries;
        _items.SelectedIndex = _entries.Count == 0 ? -1 : 0;
    }

    private static Control CreateContent(
        out TextBox search,
        out ListBox items)
    {
        search = new TextBox
        {
            PlaceholderText = "Search materials, templates, nodes…",
            Margin = new Thickness(0, 0, 0, 8),
        };
        ProfileBinding.Bind(
            search,
            TextBox.BackgroundProperty,
            Profile.RaisedResourceKey);
        ProfileBinding.Bind(
            search,
            TextBox.ForegroundProperty,
            Profile.TextResourceKey);
        ProfileBinding.Bind(
            search,
            TextBox.BorderBrushProperty,
            Profile.BorderResourceKey);
        search.SetValue(
            AutomationProperties.NameProperty,
            "Search material and asset browser");
        search.SetValue(
            AutomationProperties.AutomationIdProperty,
            "assetstudio.browser.search");

        items = new ListBox
        {
            SelectionMode = SelectionMode.Single,
            MinHeight = 220,
        };
        items.SetValue(
            AutomationProperties.NameProperty,
            "Material and asset browser entries");
        items.SetValue(
            AutomationProperties.AutomationIdProperty,
            "assetstudio.browser.items");
        items.ItemTemplate = new FuncDataTemplate<AssetStudioBrowserEntry>(
            (entry, _) =>
            {
                var title = new TextBlock
                {
                    Text = $"{entry.Kind} · {entry.DisplayName}",
                    FontWeight = FontWeight.SemiBold,
                };
                ProfileBinding.Bind(
                    title,
                    TextBlock.ForegroundProperty,
                    Profile.TextResourceKey);
                var details = new TextBlock
                {
                    Text = entry.Description,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 11,
                };
                ProfileBinding.Bind(
                    details,
                    TextBlock.ForegroundProperty,
                    Profile.MutedResourceKey);
                return new StackPanel
                {
                    Margin = new Thickness(6, 4),
                    Children = { title, details },
                };
            },
            supportsRecycling: true);

        DockPanel.SetDock(search, Dock.Top);
        return new DockPanel
        {
            Children =
            {
                search,
                new ScrollViewer
                {
                    Content = items,
                    VerticalScrollBarVisibility =
                        global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                },
            },
        };
    }
}
