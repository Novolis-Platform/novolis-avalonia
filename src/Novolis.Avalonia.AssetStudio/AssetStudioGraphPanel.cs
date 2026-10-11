using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.GraphicalProfile;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using ProfileBinding = Novolis.Avalonia.GraphicalProfile.GraphicalProfileBinding;
using Novolis.Visual.Definitions;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Graph projection with a canvas-independent accessible node and port fallback.
/// </summary>
public sealed class AssetStudioGraphPanel : AssetStudioPanel
{
    private readonly TextBlock _summary;
    private readonly AssetStudioNodePortList _nodePorts;

    public AssetStudioGraphPanel(
        VisualRecipe? recipe = null,
        VisualNodeRegistry? registry = null)
        : base(
            "Graph",
            CreateContent(
                out var summary,
                out var nodePorts),
            "assetstudio.panel.graph")
    {
        _summary = summary;
        _nodePorts = nodePorts;
        if (recipe is not null)
        {
            SetRecipe(recipe, registry);
        }
    }

    /// <summary>Accessible ordered node and port workflow.</summary>
    public AssetStudioNodePortList NodePorts => _nodePorts;

    /// <summary>Recipe currently projected by the panel.</summary>
    public VisualRecipe? Recipe { get; private set; }

    /// <summary>Replaces the semantic graph projection.</summary>
    public void SetRecipe(
        VisualRecipe recipe,
        VisualNodeRegistry? registry = null)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        Recipe = recipe;
        _nodePorts.SetRecipe(recipe, registry);
        _summary.Text =
            $"{recipe.Nodes.Length} node(s) · {recipe.Connections.Length} connection(s) · " +
            $"{recipe.Outputs.Length} output(s)";
    }

    private static Control CreateContent(
        out TextBlock summary,
        out AssetStudioNodePortList nodePorts)
    {
        summary = new TextBlock
        {
            Text = "No recipe loaded.",
            Margin = new Thickness(0, 0, 0, 8),
            TextWrapping = TextWrapping.Wrap,
        };
        ProfileBinding.Bind(
            summary,
            TextBlock.ForegroundProperty,
            Profile.MutedResourceKey);

        nodePorts = new AssetStudioNodePortList();
        var scroll = new ScrollViewer
        {
            Content = nodePorts,
            HorizontalScrollBarVisibility =
                global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
        };

        var content = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
        };
        content.Children.Add(summary);
        Grid.SetRow(scroll, 1);
        content.Children.Add(scroll);
        return content;
    }
}
