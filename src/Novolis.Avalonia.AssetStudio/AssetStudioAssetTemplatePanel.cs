using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Visual.Presets;
using Novolis.Avalonia.GraphicalProfile;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using ProfileBinding = Novolis.Avalonia.GraphicalProfile.GraphicalProfileBinding;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// No-code starter template selector for complete procedural assets.
/// </summary>
public sealed class AssetStudioAssetTemplatePanel : AssetStudioPanel
{
    private readonly ListBox _items;
    private VisualPresetCatalog _catalog = VisualPresetCatalog.CreateDefault();

    public AssetStudioAssetTemplatePanel()
        : base(
            "Asset Templates",
            CreateContent(out var items),
            "assetstudio.panel.asset-templates")
    {
        _items = items;
        _items.SelectionChanged += (_, _) =>
        {
            if (_items.SelectedItem is VisualPresetTemplate template)
            {
                TemplateSelected?.Invoke(this, template);
            }
        };
        SetCatalog(_catalog);
    }

    public event EventHandler<VisualPresetTemplate>? TemplateSelected;

    public ListBox ItemsControl => _items;

    public void SetCatalog(VisualPresetCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _items.ItemsSource = _catalog.Templates;
        _items.SelectedIndex = _catalog.Templates.Length == 0 ? -1 : 0;
    }

    private static Control CreateContent(out ListBox items)
    {
        items = new ListBox
        {
            SelectionMode = SelectionMode.Single,
            MinHeight = 140,
        };
        items.SetValue(
            AutomationProperties.NameProperty,
            "No-code asset templates");
        items.SetValue(
            AutomationProperties.AutomationIdProperty,
            "assetstudio.asset-templates.items");
        items.ItemTemplate = new FuncDataTemplate<VisualPresetTemplate>(
            (template, _) =>
            {
                var title = new TextBlock
                {
                    Text = template.DisplayName,
                    FontWeight = FontWeight.SemiBold,
                };
                ProfileBinding.Bind(
                    title,
                    TextBlock.ForegroundProperty,
                    Profile.TextResourceKey);
                var description = new TextBlock
                {
                    Text = template.Description,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 11,
                };
                ProfileBinding.Bind(
                    description,
                    TextBlock.ForegroundProperty,
                    Profile.MutedResourceKey);
                return new StackPanel
                {
                    Margin = new Thickness(6, 4),
                    Children = { title, description },
                };
            },
            supportsRecycling: true);
        return new ScrollViewer
        {
            Content = items,
            VerticalScrollBarVisibility =
                global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        };
    }
}
