using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Visual.Compile;
using Novolis.Avalonia.GraphicalProfile;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using ProfileBinding = Novolis.Avalonia.GraphicalProfile.GraphicalProfileBinding;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Displays truthful target capability decisions for the current graph.
/// </summary>
public sealed class AssetStudioCapabilityStatusPanel : AssetStudioPanel
{
    private readonly ListBox _items;

    public AssetStudioCapabilityStatusPanel()
        : base(
            "Target Capabilities",
            CreateContent(out var items),
            "assetstudio.panel.capabilities")
    {
        _items = items;
        SetReport(null);
    }

    public void SetReport(VisualCapabilityReport? report)
    {
        _items.ItemsSource = report is null
            ? ["Compile a material to see target capability decisions."]
            : report.Results
                .Select(
                    result =>
                        $"{result.Status} · {result.Id}" +
                        (string.IsNullOrWhiteSpace(result.Message)
                            ? string.Empty
                            : $" · {result.Message}"))
                .ToArray();
    }

    private static Control CreateContent(out ListBox items)
    {
        items = new ListBox
        {
            MinHeight = 80,
        };
        items.SetValue(
            AutomationProperties.NameProperty,
            "Target capability decisions");
        items.SetValue(
            AutomationProperties.AutomationIdProperty,
            "assetstudio.capabilities.items");
        items.ItemTemplate = new FuncDataTemplate<string>(
            (text, _) =>
            {
                var row = new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(4, 2),
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
            Content = items,
            VerticalScrollBarVisibility =
                global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        };
    }
}
