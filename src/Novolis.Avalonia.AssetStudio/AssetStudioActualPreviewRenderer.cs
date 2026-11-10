using System.Collections.Immutable;
using System.Numerics;
using Novolis.Math.Geometry;
using Novolis.Rendering.Backends.Cpu;
using Novolis.Rendering.Backends.Igpu;
using Novolis.Rendering.Compile;
using Novolis.Rendering.Materials;
using Novolis.Rendering.PathTrace.Demos;
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
/// Renders Asset Studio previews through ILGPU path tracing (CPU fallback),
/// with softbox lighting and mild low-spp denoise — same stack as SilkTraceStudio.
/// </summary>
internal static class AssetStudioActualPreviewRenderer
{
    private const int DefaultWidth = 480;
    private const int DefaultHeight = 360;
    private const int GpuPreviewSamples = 32;
    private const int CpuPreviewSamples = 12;
    private const int PreviewMaxDepth = 4;

    public static byte[] Render(
        VisualIR compilation,
        string shape,
        int width = DefaultWidth,
        int height = DefaultHeight)
    {
        var material = ResolveRenderMaterial(compilation);
        var useStrip = shape.Contains("Strip", StringComparison.OrdinalIgnoreCase);
        var scene = useStrip
            ? BuildMaterialStudioScene(
                material,
                subject: static (builder, mat) => builder.AddBox(
                    new Vector3(0f, 0.09f, 0f),
                    new Vector3(1.6f, 0.09f, 0.95f),
                    mat),
                subjectHalfHeight: 0.09f)
            : BuildMaterialStudioScene(
                material,
                subject: static (builder, mat) =>
                {
                    AddUvSphere(builder, center: new Vector3(0f, 0.95f, 0f), radius: 0.95f, material: mat);
                },
                subjectHalfHeight: 0.95f);

        return RenderScene(
            scene,
            new Vector3(-1.6f, 0f, -1.6f),
            new Vector3(1.6f, 2.2f, 1.6f),
            width,
            height);
    }

    public static byte[] RenderAsset(
        VisualIR compilation,
        AssetRecipe asset,
        AssetInstance? instance = null,
        int width = DefaultWidth,
        int height = DefaultHeight)
    {
        ArgumentNullException.ThrowIfNull(asset);
        var material = ResolveRenderMaterial(compilation);
        var materials = asset.MaterialSlots.ToDictionary(
            slot => slot.MaterialId,
            _ => material,
            StringComparer.Ordinal);
        var preview = VisualAssetSceneAdapter.Convert(
            asset,
            instance,
            materials,
            material);
        if (preview.Rendering.Scene is null)
        {
            throw new InvalidOperationException(
                string.Join(
                    Environment.NewLine,
                    preview.Diagnostics.Select(diagnostic => diagnostic.Message)));
        }

        var studio = WrapAssetInStudio(
            preview.Rendering.Scene,
            preview.BoundsMinimum,
            preview.BoundsMaximum);
        return RenderScene(
            studio.Scene,
            studio.Minimum,
            studio.Maximum,
            width,
            height);
    }

    public static byte[] RenderGeometryPreview(
        int width = DefaultWidth,
        int height = DefaultHeight,
        string label = "Geometry output")
    {
        _ = label;
        var material = new StandardMaterial
        {
            BaseColor = new Vector3(0.42f, 0.46f, 0.54f),
            Roughness = 0.42f,
            Metallic = 0.12f,
        };
        var scene = BuildMaterialStudioScene(
            material,
            subject: static (builder, mat) => builder.AddBox(
                new Vector3(0f, 0.85f, 0f),
                new Vector3(0.85f, 0.85f, 0.85f),
                mat),
            subjectHalfHeight: 0.85f);
        return RenderScene(
            scene,
            new Vector3(-1.2f, 0f, -1.2f),
            new Vector3(1.2f, 2.2f, 1.2f),
            width,
            height);
    }

    private static IMaterial ResolveRenderMaterial(VisualIR compilation)
    {
        var material = EvaluateMaterial(compilation) ??
                       new MaterialValue(
                           "preview.default",
                           VisualMaterialModel.StandardPbr,
                           new ColorValue(0.55, 0.58, 0.64),
                           roughness: 0.35);
        var lowered = RenderingMaterialAdapter.LowerMaterial(material);
        return lowered.Material ??
               new StandardMaterial
               {
                   BaseColor = new Vector3(0.55f, 0.58f, 0.64f),
                   Roughness = 0.35f,
               };
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

    /// <summary>
    /// Subject + checker floor + ceiling softbox — same lighting pattern as SilkTraceStudio.
    /// </summary>
    private static Scene BuildMaterialStudioScene(
        IMaterial subjectMaterial,
        Action<SceneBuilder, IMaterial> subject,
        float subjectHalfHeight)
    {
        var softboxY = MathF.Max(2.6f, subjectHalfHeight * 2f + 1.4f);
        var builder = new SceneBuilder()
            .AddGround(MaterialPresets.Showcase.CheckerFloor(), size: 6f);
        subject(builder, subjectMaterial);
        builder
            .AddBox(
                new Vector3(0f, softboxY, 0.1f),
                new Vector3(1.2f, 0.02f, 0.9f),
                MaterialPresets.Showcase.Softbox())
            .AddDirectionalLight(
                new Vector3(-0.2f, -1f, -0.15f),
                new Vector3(1f, 0.97f, 0.94f),
                0.15f);
        return builder.Build();
    }

    private static (Scene Scene, Vector3 Minimum, Vector3 Maximum) WrapAssetInStudio(
        Scene assetScene,
        Vector3 minimum,
        Vector3 maximum)
    {
        var extent = maximum - minimum;
        var span = MathF.Max(extent.X, MathF.Max(extent.Y, extent.Z));
        var groundSize = MathF.Max(4f, span * 3.5f);
        var softboxY = maximum.Y + MathF.Max(1.2f, span * 0.75f);
        var softHalf = MathF.Max(0.8f, span * 0.55f);
        var builder = new SceneBuilder()
            .AddGround(MaterialPresets.Showcase.CheckerFloor(), size: groundSize);
        foreach (var mesh in assetScene.Meshes)
        {
            builder.AddMesh(
                mesh.Vertices.ToArray(),
                mesh.TriangleIndices.ToArray(),
                mesh.Material,
                mesh.Transform);
        }

        builder
            .AddBox(
                new Vector3(
                    (minimum.X + maximum.X) * 0.5f,
                    softboxY,
                    (minimum.Z + maximum.Z) * 0.5f),
                new Vector3(softHalf, 0.02f, softHalf * 0.75f),
                MaterialPresets.Showcase.Softbox())
            .AddDirectionalLight(
                new Vector3(-0.2f, -1f, -0.15f),
                new Vector3(1f, 0.97f, 0.94f),
                0.15f);

        var pad = groundSize * 0.5f;
        var centerXz = new Vector3(
            (minimum.X + maximum.X) * 0.5f,
            0f,
            (minimum.Z + maximum.Z) * 0.5f);
        return (
            builder.Build(),
            new Vector3(centerXz.X - pad, 0f, centerXz.Z - pad),
            new Vector3(centerXz.X + pad, softboxY + 0.1f, centerXz.Z + pad));
    }

    private static void AddUvSphere(
        SceneBuilder builder,
        Vector3 center,
        float radius,
        IMaterial material,
        int segments = 28,
        int rings = 16)
    {
        segments = System.Math.Max(8, segments);
        rings = System.Math.Max(4, rings);
        var verts = new List<Vector3>((rings + 1) * (segments + 1));
        var indices = new List<int>(rings * segments * 6);
        for (var ring = 0; ring <= rings; ring++)
        {
            var v = ring / (float)rings;
            var phi = v * MathF.PI;
            var y = MathF.Cos(phi);
            var ringRadius = MathF.Sin(phi);
            for (var seg = 0; seg <= segments; seg++)
            {
                var u = seg / (float)segments;
                var theta = u * MathF.PI * 2f;
                verts.Add(
                    center + new Vector3(
                        ringRadius * MathF.Cos(theta),
                        y,
                        ringRadius * MathF.Sin(theta)) * radius);
            }
        }

        var stride = segments + 1;
        for (var ring = 0; ring < rings; ring++)
        {
            for (var seg = 0; seg < segments; seg++)
            {
                var i0 = ring * stride + seg;
                var i1 = i0 + 1;
                var i2 = i0 + stride;
                var i3 = i2 + 1;
                indices.Add(i0);
                indices.Add(i2);
                indices.Add(i1);
                indices.Add(i1);
                indices.Add(i2);
                indices.Add(i3);
            }
        }

        builder.AddMesh(verts.ToArray(), indices.ToArray(), material);
    }

    private static byte[] RenderScene(
        Scene scene,
        Vector3 minimum,
        Vector3 maximum,
        int width,
        int height)
    {
        width = System.Math.Max(64, width);
        height = System.Math.Max(64, height);
        var compiled = SceneCompiler.Compile(scene);
        var center = (minimum + maximum) * 0.5f;
        var extent = maximum - minimum;
        var radius = MathF.Max(
            0.5f,
            MathF.Max(extent.X, MathF.Max(extent.Y, extent.Z)) * 0.5f);
        var distance = radius * 2.8f;
        var camera = CameraSnapshot.LookAt(
            center + new Vector3(radius * 0.75f, radius * 0.55f, distance),
            center,
            Vector3.UnitY,
            42f,
            width / (float)height);

        var backend = CreatePreviewBackend(out var sampleCount, out var backendLabel);
        try
        {
            if (backend is ITraceDepthControl depth)
            {
                depth.MaxTraceDepth = PreviewMaxDepth;
            }

            backend.ResizeAsync(width, height).GetAwaiter().GetResult();
            backend.UploadSceneAsync(compiled).GetAwaiter().GetResult();
            backend.ResetAccumulation();
            RenderSamples(backend, camera, sampleCount);

            if (!backend.Output.TryGetCpuPixels(
                    out var pixels,
                    out var renderedWidth,
                    out var renderedHeight))
            {
                throw new InvalidOperationException(
                    $"The preview backend ({backendLabel}) did not expose an image.");
            }

            var frame = pixels.ToArray();
            if (sampleCount < PathTraceMildDenoise.MaxSampleCount)
            {
                var scratch = new Rgba32[frame.Length];
                PathTraceMildDenoise.ApplyInPlace(
                    frame,
                    renderedWidth,
                    renderedHeight,
                    sampleCount,
                    scratch);
            }

            var converted = ImmutableArray.CreateBuilder<VisualRgba32>(frame.Length);
            foreach (var pixel in frame)
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
        finally
        {
            if (backend is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    private static IRayTracingBackend CreatePreviewBackend(
        out int sampleCount,
        out string backendLabel)
    {
        var gpu = new IlgpuRayTracingBackend(deterministic: false);
        backendLabel = gpu.BackendLabel;
        if (IsRealGpu(backendLabel))
        {
            sampleCount = GpuPreviewSamples;
            return gpu;
        }

        gpu.Dispose();
        sampleCount = CpuPreviewSamples;
        backendLabel = "CPU (deterministic)";
        return new CpuRayTracingBackend(deterministic: true);
    }

    private static void RenderSamples(
        IRayTracingBackend backend,
        CameraSnapshot camera,
        int sampleCount)
    {
        if (backend is IlgpuRayTracingBackend igpu)
        {
            igpu.RenderBatchAsync(camera, 0, sampleCount, flushToCpu: true)
                .GetAwaiter()
                .GetResult();
            return;
        }

        if (backend is CpuRayTracingBackend cpu)
        {
            cpu.RenderSamples(camera, 0, sampleCount);
            return;
        }

        for (var i = 0; i < sampleCount; i++)
        {
            backend.RenderAsync(camera, i).GetAwaiter().GetResult();
        }
    }

    private static bool IsRealGpu(string label) =>
        label.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)
        || label.Contains("GeForce", StringComparison.OrdinalIgnoreCase)
        || label.Contains("Radeon", StringComparison.OrdinalIgnoreCase)
        || label.Contains("Arc", StringComparison.OrdinalIgnoreCase)
        || (label.StartsWith("ILGPU (", StringComparison.Ordinal)
            && !label.Contains("fallback", StringComparison.OrdinalIgnoreCase)
            && !label.Contains("CPU", StringComparison.OrdinalIgnoreCase));
}
