using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Visual.Assets;
using Novolis.Visual.Materials;
using Novolis.Avalonia.GraphicalProfile;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using ProfileBinding = Novolis.Avalonia.GraphicalProfile.GraphicalProfileBinding;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Inheritance and override summary for reusable material instances.
/// </summary>
public sealed class AssetStudioInstancePanel : AssetStudioPanel
{
    private readonly TextBlock _summary;

    public AssetStudioInstancePanel()
        : base(
            "Reusable Instance",
            CreateContent(out var summary),
            "assetstudio.panel.instance")
    {
        _summary = summary;
    }

    public void SetInstance(
        MaterialGraph? graph,
        MaterialInstance? instance)
    {
        if (graph is null || instance is null)
        {
            _summary.Text =
                "Choose a material to create a child instance with inherited controls.";
            return;
        }

        var overridden = instance.Overrides.Count == 0
            ? "inherits all parent values"
            : $"{instance.Overrides.Count} override(s)";
        _summary.Text =
            $"{instance.InstanceId}\nParent: {graph.DisplayName}\n{overridden}\n" +
            "Reset controls from the inspector to return to inheritance.";
    }

    /// <summary>Projects an asset instance through the same inheritance surface.</summary>
    public void SetAssetInstance(
        AssetRecipe? asset,
        AssetInstance? instance)
    {
        if (asset is null || instance is null)
        {
            _summary.Text =
                "Choose an asset to create a child instance with inherited controls.";
            return;
        }

        var overridden = instance.Overrides.Count == 0
            ? "inherits all parent values"
            : $"{instance.Overrides.Count} override(s)";
        _summary.Text =
            $"{instance.InstanceId}\nParent: {asset.DisplayName}\n{overridden}\n" +
            "Reset controls from the inspector to return to inheritance.";
    }

    private static Control CreateContent(out TextBlock summary)
    {
        summary = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 80,
        };
        ProfileBinding.Bind(
            summary,
            TextBlock.ForegroundProperty,
            Profile.MutedResourceKey);
        summary.SetValue(
            AutomationProperties.NameProperty,
            "Reusable material instance summary");
        summary.SetValue(
            AutomationProperties.AutomationIdProperty,
            "assetstudio.instance.summary");
        return new Border
        {
            Padding = new Thickness(4),
            Child = summary,
        };
    }
}
