using Novolis.AssetStudio.Authoring;
using Novolis.Avalonia.AssetStudio;
using Novolis.Visual.Compile;
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

    [Test]
    public async Task Material_preview_returns_lit_reference_bytes()
    {
        var material = VisualPresetCatalog.CreateDefault()
            .Find("lightsaber-blade")!
            .Materials
            .Single();
        var compilation = VisualCompiler.Compile(
            material.Recipe,
            new VisualCompileOptions(
                registry: VisualCompiler.CreateDefaultRegistry(),
                strict: true));
        var service = new CpuAssetStudioPreviewService(
            VisualCompiler.CreateDefaultRegistry());

        var result = await service.RequestAsync(
            new AssetStudioSemanticState(material.Recipe));

        await Assert.That(compilation.Success).IsTrue();
        await Assert.That(result.Status).IsEqualTo("Ready");
        await Assert.That(result.PngBytes).IsNotNull();
        await Assert.That(result.PngBytes!.Length).IsGreaterThan(100);
    }

    [Test]
    public async Task Asset_preview_renders_generic_component_geometry_to_png()
    {
        var template = VisualPresetCatalog.CreateDefault()
            .Find("orbital-reusable-rocket")!;
        var material = template.Materials.First();
        var compilation = VisualCompiler.Compile(
            material.Recipe,
            new VisualCompileOptions(
                registry: VisualCompiler.CreateDefaultRegistry(),
                strict: true));

        var png = AssetStudioActualPreviewRenderer.RenderAsset(
            compilation,
            template.Asset);

        await Assert.That(png.Length).IsGreaterThan(100);
    }

}
