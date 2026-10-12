using Novolis.Avalonia.AssetStudio;
using Novolis.Visual.Presets;

namespace Novolis.Avalonia.AssetStudio.Unit;

public sealed class MaterialAssetStudioPanelTests
{
    [Test]
    public async Task Browser_exposes_all_no_code_asset_templates_and_assets()
    {
        var catalog = VisualPresetCatalog.CreateDefault();
        var browser = new AssetStudioMaterialBrowserPanel();
        browser.SetCatalogs(
            presets: catalog,
            assets: catalog.Templates.Select(template => template.Asset));

        await Assert.That(
                browser.Entries
                    .Count(entry => entry.Kind == "Template"))
            .IsEqualTo(4);
        await Assert.That(
                browser.Entries
                    .Count(entry => entry.Kind == "Asset"))
            .IsEqualTo(4);
    }

    [Test]
    public async Task Asset_inspector_uses_exposed_parameter_metadata()
    {
        var template = VisualPresetCatalog.CreateDefault()
            .Find("neon-sign")!;
        var inspector = new AssetStudioAssetInspectorPanel();
        inspector.SetAsset(template.Asset);

        await Assert.That(inspector.Asset).IsEqualTo(template.Asset);
        await Assert.That(inspector.Instance).IsNotNull();
        await Assert.That(
                inspector.Instance!.Resolve("sign.text").Kind)
            .IsEqualTo(Novolis.Visual.Definitions.VisualValueKind.Text);
    }
}
