using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.GraphicalProfile;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using ProfileBinding = Novolis.Avalonia.GraphicalProfile.GraphicalProfileBinding;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Ordered stack projection for the current visual recipe.
/// </summary>
public sealed class AssetStudioStackPanel : AssetStudioPanel
{
    private readonly ListBox _items;
    private IReadOnlyList<string> _stackItems = [];

    public AssetStudioStackPanel(IEnumerable<string>? items = null)
        : base(
            "Stack",
            CreateContent(out var list),
            "assetstudio.panel.stack")
    {
        _items = list;
        SetItems(items ?? []);
    }

    /// <summary>List control used by keyboard and assistive workflows.</summary>
    public ListBox ItemsControl => _items;

    /// <summary>Current ordered stack labels.</summary>
    public IReadOnlyList<string> Items => _stackItems;

    /// <summary>Replaces the stack projection.</summary>
    public void SetItems(IEnumerable<string> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        _stackItems = items
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();
        _items.ItemsSource = _stackItems;
        _items.SelectedIndex = _stackItems.Count == 0 ? -1 : 0;
    }

    private static Control CreateContent(out ListBox list)
    {
        list = new ListBox
        {
            SelectionMode = SelectionMode.Single,
            MinHeight = 120,
        };
        list.SetValue(
            AutomationProperties.NameProperty,
            "Ordered visual stack");
        list.SetValue(
            AutomationProperties.AutomationIdProperty,
            "assetstudio.stack.items");
        list.ItemTemplate = new FuncDataTemplate<string>(
            (item, _) =>
            {
                var row = new TextBlock
                {
                    Text = item,
                    Margin = new Thickness(6, 4),
                    TextWrapping = TextWrapping.Wrap,
                };
                ProfileBinding.Bind(
                    row,
                    TextBlock.ForegroundProperty,
                    Profile.TextResourceKey);
                return row;
            },
            supportsRecycling: true);

        return new ScrollViewer
        {
            Content = list,
            HorizontalScrollBarVisibility =
                global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
        };
    }
}
