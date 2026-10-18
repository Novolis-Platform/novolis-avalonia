using System.Collections.Immutable;
using System.Numerics;
using Novolis.Modeling;
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Compile;
using Novolis.Rendering.Materials;
using Novolis.Rendering.Runtime;
using Novolis.Rendering.Scene;
using Novolis.Visual.Adapters.Rendering;
using Novolis.Visual.Assets;
using Novolis.Visual.Baking;
using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;
using Novolis.Visual.Evaluation;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Renders previews through Modeling, the Visual rendering adapter, Scene
/// compilation, and the deterministic CPU path tracer. No catalog entry or
/// asset identifier is interpreted here.
/// </summary>
internal static class AssetStudioActualPreviewRenderer
{
    public static byte[] Render(
        VisualIR compilation,
        string shape,
        int width = 360,
        int height = 260)
    {
        var material = EvaluateMaterial(compilation) ??
                       new MaterialValue(
                           "preview.default",
                           VisualMaterialModel.StandardPbr,
                           new ColorValue(0.55, 0.58, 0.64),
                           roughness: 0.35);
        var lowered = RenderingMaterialAdapter.LowerMaterial(material);
        var renderMaterial = lowered.Material ??
                             new StandardMaterial
                             {
                                 BaseColor = new Vector3(0.55f, 0.58f, 0.64f),
                                 Roughness = 0.35f,
                             };
        var primitive = shape.Contains(
            "Strip",
            StringComparison.OrdinalIgnoreCase)
            ? MeshPrimitiveKind.Box
            : MeshPrimitiveKind.Sphere;
        var size = primitive == MeshPrimitiveKind.Box
            ? new Vector3(3.2f, 0.18f, 1.9f)
            : new Vector3(1.9f);
        var document = CreatePrimitiveDocument(primitive, size);
        var preview = ConvertDocument(
            document,
            renderMaterial,
            "material-preview");
        return RenderScene(
            preview.Scene,
            new Vector3(-size.X * 0.5f, -size.Y * 0.5f, -size.Z * 0.5f),
            new Vector3(size.X * 0.5f, size.Y * 0.5f, size.Z * 0.5f),
            width,
            height);
    }

    public static byte[] RenderAsset(
        VisualIR compilation,
        AssetRecipe asset,
        AssetInstance? instance = null,
        int width = 360,
        int height = 260)
    {
        ArgumentNullException.ThrowIfNull(asset);
        var material = EvaluateMaterial(compilation) ??
                       new MaterialValue(
                           "preview.default",
                           VisualMaterialModel.StandardPbr,
                           new ColorValue(0.55, 0.58, 0.64),
                           roughness: 0.35);
        var lowered = RenderingMaterialAdapter.LowerMaterial(material);
        var renderMaterial = lowered.Material ??
                             new StandardMaterial
                             {
                                 BaseColor = new Vector3(0.55f, 0.58f, 0.64f),
                                 Roughness = 0.35f,
                             };
        var materials = asset.MaterialSlots.ToDictionary(
            slot => slot.MaterialId,
            _ => renderMaterial,
            StringComparer.Ordinal);
        var preview = VisualAssetSceneAdapter.Convert(
            asset,
            instance,
            materials,
            renderMaterial);
        if (preview.Rendering.Scene is null)
        {
            throw new InvalidOperationException(
                string.Join(
                    Environment.NewLine,
                    preview.Diagnostics.Select(diagnostic => diagnostic.Message)));
        }

        return RenderScene(
            preview.Rendering.Scene,
            preview.BoundsMinimum,
            preview.BoundsMaximum,
            width,
            height);
    }

    public static byte[] RenderGeometryPreview(
        int width = 360,
        int height = 260,
        string label = "Geometry output")
    {
        var material = new StandardMaterial
        {
            BaseColor = new Vector3(0.42f, 0.46f, 0.54f),
            Roughness = 0.42f,
            Metallic = 0.12f,
        };
        var document = CreatePrimitiveDocument(
            MeshPrimitiveKind.Box,
            new Vector3(1.7f, 1.7f, 1.7f));
        var preview = ConvertDocument(document, material, label);
        return RenderScene(
            preview.Scene,
            new Vector3(-0.85f),
            new Vector3(0.85f),
            width,
            height);
    }

    private static MaterialValue? EvaluateMaterial(VisualIR compilation)
    {
        ArgumentNullException.ThrowIfNull(compilation);
        if (!compilation.Success)
        {
            return null;
        }

        var output = compilation.Outputs.FirstOrDefault(
            candidate => candidate.Contract.ValueKind == VisualValueKind.Material);
        if (output is null)
        {
            return null;
        }

        var result = VisualEvaluator.Evaluate(
            compilation,
            options: new VisualEvaluationOptions());
        return result.TryGetOutput(output.Id, out var value)
            ? value as MaterialValue
            : null;
    }

    private static SceneDocument CreatePrimitiveDocument(
        MeshPrimitiveKind primitive,
        Vector3 size)
    {
        var document = new SceneDocument
        {
            Name = "Asset Studio preview",
            Generator = "Novolis.Avalonia.AssetStudio",
        };
        document.Nodes.Add(
            new MeshNode
            {
                Name = "Preview geometry",
                Primitive = primitive,
                Size = [size.X, size.Y, size.Z],
                Segments = 32,
            });
        return document;
    }

    private static RenderingSceneConversion ConvertDocument(
        SceneDocument document,
        IMaterial material,
        string sourcePath)
    {
        var evaluator = new SceneEvaluator();
        evaluator.Bind(document);
        var meshes = evaluator.Cache.EvaluatedMeshes.Select(
            mesh => new RenderingMeshInput(mesh, material, sourcePath));
        return VisualRenderingAdapter.ConvertScene(
            meshes,
            [
                new LightDefinition(
                    Novolis.Rendering.Scene.LightKind.Directional,
                    Vector3.Normalize(new Vector3(-0.45f, -0.7f, -0.35f)),
                    new Vector3(1f, 0.94f, 0.86f),
                    3.5f),
                new LightDefinition(
                    Novolis.Rendering.Scene.LightKind.Point,
                    new Vector3(-3f, 4f, 4f),
                    new Vector3(0.48f, 0.65f, 1f),
                    80f),
                new LightDefinition(
                    Novolis.Rendering.Scene.LightKind.Point,
                    new Vector3(3f, 2f, -2f),
                    new Vector3(1f, 0.28f, 0.1f),
                    35f),
            ]);
    }

    private static byte[] RenderScene(
        Scene? scene,
        Vector3 minimum,
        Vector3 maximum,
        int width,
        int height)
    {
        if (scene is null)
        {
            throw new InvalidOperationException(
                "The preview scene could not be lowered to Rendering.");
        }

        var compiled = SceneCompiler.Compile(scene);
        var backend = new CpuRayTracingBackend(deterministic: true);
        backend.ResizeAsync(width, height).GetAwaiter().GetResult();
        backend.UploadSceneAsync(compiled).GetAwaiter().GetResult();

        var center = (minimum + maximum) * 0.5f;
        var extent = maximum - minimum;
        var radius = MathF.Max(
            0.5f,
            MathF.Max(extent.X, MathF.Max(extent.Y, extent.Z)) * 0.5f);
        var distance = radius * 2.8f;
        var camera = CameraSnapshot.LookAt(
            center + new Vector3(radius * 0.75f, radius * 0.35f, distance),
            center,
            Vector3.UnitY,
            42f,
            width / (float)height);
        backend.RenderSamples(camera, 0, 2);
        if (!backend.Output.TryGetCpuPixels(
                out var pixels,
                out var renderedWidth,
                out var renderedHeight))
        {
            throw new InvalidOperationException(
                "The CPU preview backend did not expose an image.");
        }

        var converted = ImmutableArray.CreateBuilder<VisualRgba32>(
            renderedWidth * renderedHeight);
        foreach (var pixel in pixels)
        {
            converted.Add(new VisualRgba32(pixel.R, pixel.G, pixel.B, pixel.A));
        }

        return new VisualBakeImage
        {
            Width = renderedWidth,
            Height = renderedHeight,
            Pixels = converted.ToImmutable(),
        }.ToPng();
    }
}
