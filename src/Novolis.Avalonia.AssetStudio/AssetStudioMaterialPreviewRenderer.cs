using System.Collections.Immutable;
using System.Numerics;
using Novolis.Visual.Assets;
using Novolis.Visual.Compile;
using Novolis.Visual.Baking;
using Novolis.Visual.Definitions;
using Novolis.Visual.Evaluation;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Small deterministic CPU material viewport used when a GPU renderer is not
/// available. It is deliberately a lit presentation surface rather than a
/// flat channel swatch, so roughness, metallic, transmission, clearcoat, and
/// emission are visible while authoring.
/// </summary>
internal static class AssetStudioMaterialPreviewRenderer
{
    public static byte[] Render(
        VisualIR compilation,
        string shape,
        int width = 360,
        int height = 260)
    {
        ArgumentNullException.ThrowIfNull(compilation);
        if (!compilation.Success)
        {
            throw new InvalidOperationException(
                "A successful compilation is required for a material preview.");
        }

        var output = compilation.Outputs.FirstOrDefault(
            candidate => candidate.Contract.ValueKind == VisualValueKind.Material)
            ?? compilation.Outputs.FirstOrDefault();
        if (output is null)
        {
            return RenderStudioPlaceholder(width, height, "No output");
        }

        var pixels = ImmutableArray.CreateBuilder<VisualRgba32>(width * height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                pixels.Add(
                    EvaluatePixel(
                        compilation,
                        output,
                        shape,
                        x,
                        y,
                        width,
                        height));
            }
        }

        return new VisualBakeImage
        {
            Width = width,
            Height = height,
            Pixels = pixels.ToImmutable(),
        }.ToPng();
    }

    public static byte[] RenderAsset(
        VisualIR compilation,
        AssetRecipe asset,
        AssetInstance? instance = null,
        int width = 360,
        int height = 260)
    {
        ArgumentNullException.ThrowIfNull(compilation);
        ArgumentNullException.ThrowIfNull(asset);
        if (!compilation.Success)
        {
            throw new InvalidOperationException(
                "A successful compilation is required for an asset preview.");
        }

        var output = compilation.Outputs.FirstOrDefault(
            candidate => candidate.Contract.ValueKind == VisualValueKind.Material)
            ?? compilation.Outputs.FirstOrDefault();
        if (output is null)
        {
            return RenderStudioPlaceholder(width, height, asset.DisplayName);
        }

        var pixels = ImmutableArray.CreateBuilder<VisualRgba32>(width * height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                pixels.Add(
                    EvaluateAssetPixel(
                        compilation,
                        output,
                        asset,
                        instance,
                        x,
                        y,
                        width,
                        height));
            }
        }

        return new VisualBakeImage
        {
            Width = width,
            Height = height,
            Pixels = pixels.ToImmutable(),
        }.ToPng();
    }

    public static byte[] RenderStudioPlaceholder(
        int width = 360,
        int height = 260,
        string label = "Geometry output")
    {
        var pixels = ImmutableArray.CreateBuilder<VisualRgba32>(width * height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var u = width == 1 ? 0f : x / (float)(width - 1);
                var v = height == 1 ? 0f : y / (float)(height - 1);
                var background = new Vector3(
                    0.035f + 0.06f * (1f - v),
                    0.05f + 0.07f * (1f - v),
                    0.08f + 0.1f * (1f - v));
                var grid = (Math.Abs((u * 12f) % 1f - 0.5f) < 0.012f ||
                            Math.Abs((v * 8f) % 1f - 0.5f) < 0.012f)
                    ? 0.08f
                    : 0f;
                var silhouette = RocketSilhouette(u, v);
                var color = background + new Vector3(grid);
                if (silhouette > 0f)
                {
                    var light = 0.35f + 0.65f * (1f - v);
                    color = Vector3.Lerp(
                        color,
                        new Vector3(0.48f, 0.58f, 0.72f) * light,
                        silhouette);
                }

                pixels.Add(ToPixel(color, 1f));
            }
        }

        return new VisualBakeImage
        {
            Width = width,
            Height = height,
            Pixels = pixels.ToImmutable(),
        }.ToPng();
    }

    private static VisualRgba32 EvaluatePixel(
        VisualIR compilation,
        VisualIROutput output,
        string shape,
        int x,
        int y,
        int width,
        int height)
    {
        var u = width == 1 ? 0f : x / (float)(width - 1);
        var v = height == 1 ? 0f : y / (float)(height - 1);
        var background = StudioBackground(u, v);
        var centeredX = u * 2f - 1f;
        var centeredY = v * 2f - 1f;
        var radius = shape.Contains("Strip", StringComparison.OrdinalIgnoreCase)
            ? 0.84f
            : 0.78f;

        var localX = centeredX / radius;
        var localY = centeredY / radius;
        var distanceSquared = localX * localX + localY * localY;
        if (distanceSquared > 1f)
        {
            return ToPixel(background, 1f);
        }

        var z = MathF.Sqrt(MathF.Max(0f, 1f - distanceSquared));
        var normal = Vector3.Normalize(new Vector3(localX, -localY, z));
        var context = new VisualEvaluationContext
        {
            Position = new Vector3(localX, -localY, z),
            Uv = new Vector2(u, v),
            Normal = normal,
            Time = 0d,
            Seed = 17,
        };
        var material = EvaluateMaterial(compilation, output, context);
        if (material is null)
        {
            return ToPixel(background, 1f);
        }

        var shaded = Shade(material, normal, background);
        return ToPixel(shaded, (float)material.Alpha);
    }

    private static VisualRgba32 EvaluateAssetPixel(
        VisualIR compilation,
        VisualIROutput output,
        AssetRecipe asset,
        AssetInstance? instance,
        int x,
        int y,
        int width,
        int height)
    {
        var u = width == 1 ? 0f : x / (float)(width - 1);
        var v = height == 1 ? 0f : y / (float)(height - 1);
        var background = StudioBackground(u, v);
        var centeredX = u * 2f - 1f;
        var centeredY = v * 2f - 1f;
        var normal = Vector3.UnitZ;
        var usesMaterial = false;
        var coverage = 0f;
        var accent = Vector3.Zero;

        switch (asset.AssetId)
        {
            case "asset.lightsaber.blade":
            {
                var length = Math.Clamp(
                    (float)(GetScalar(asset, instance, "blade.length", 3.2) / 12d),
                    0.12f,
                    0.68f);
                var radius = Math.Clamp(
                    (float)(GetScalar(asset, instance, "blade.radius", 0.08) / 0.5d),
                    0.035f,
                    0.16f);
                var bladeTop = 0.12f + (0.68f - length);
                var bladeBottom = 0.8f;
                var bladeDistance = MathF.Abs(centeredX) / radius;
                if (centeredY >= bladeTop &&
                    centeredY <= bladeBottom &&
                    bladeDistance <= 1f)
                {
                    var z = MathF.Sqrt(MathF.Max(0f, 1f - bladeDistance * bladeDistance));
                    normal = Vector3.Normalize(
                        new Vector3(centeredX / radius, 0f, z));
                    coverage = 1f;
                    usesMaterial = true;
                }
                else if (centeredY > 0.78f &&
                         centeredY < 0.97f &&
                         MathF.Abs(centeredX) < 0.13f)
                {
                    coverage = 1f;
                    accent = new Vector3(0.08f, 0.1f, 0.14f);
                }
                else if (centeredY >= bladeTop - 0.05f &&
                         centeredY <= bladeBottom + 0.05f &&
                         bladeDistance <= 1.45f)
                {
                    coverage = 0.22f;
                    accent = new Vector3(0.08f, 0.3f, 0.6f);
                }

                break;
            }
            case "asset.neon.sign":
            {
                var backing = MathF.Abs(centeredX) < 0.84f &&
                              MathF.Abs(centeredY) < 0.55f;
                var tube = NeonTube(centeredX, centeredY, instance);
                if (tube)
                {
                    coverage = 1f;
                    usesMaterial = true;
                }
                else if (backing)
                {
                    coverage = 1f;
                    accent = new Vector3(0.025f, 0.03f, 0.05f);
                }

                break;
            }
            case "asset.glass.sample":
            {
                var pane = MathF.Abs(centeredX) < 0.7f &&
                           MathF.Abs(centeredY) < 0.62f;
                if (pane)
                {
                    var bevel = MathF.Max(
                        MathF.Abs(centeredX) / 0.7f,
                        MathF.Abs(centeredY) / 0.62f);
                    normal = Vector3.Normalize(
                        new Vector3(
                            centeredX * 0.25f,
                            -centeredY * 0.25f,
                            MathF.Max(0.2f, 1f - bevel * 0.2f)));
                    coverage = 1f;
                    usesMaterial = true;
                }

                break;
            }
            case "asset.orbital.reusable-rocket":
            {
                var diameter = Math.Clamp(
                    (float)(GetScalar(asset, instance, "rocket.bodyDiameter", 2.4) / 12d),
                    0.12f,
                    0.32f);
                var body = MathF.Abs(centeredX) < diameter &&
                           centeredY > 0.2f &&
                           centeredY < 0.78f;
                var nose = centeredY <= 0.3f &&
                           MathF.Abs(centeredX) < (0.3f - centeredY) * 1.7f;
                var fins = centeredY > 0.68f &&
                           centeredY < 0.84f &&
                           MathF.Abs(centeredX) < 0.38f - centeredY * 0.12f;
                var windows = centeredY > 0.35f &&
                              centeredY < 0.52f &&
                              MathF.Abs(centeredX) < diameter * 1.1f &&
                              MathF.Abs(centeredX * 11f) % 1f < 0.35f;
                var engines = centeredY > 0.78f &&
                              centeredY < 0.9f &&
                              MathF.Abs(centeredX) < 0.24f;
                if (windows)
                {
                    coverage = 1f;
                    accent = new Vector3(0.04f, 0.25f, 0.8f);
                }
                else if (body || nose || fins)
                {
                    coverage = 1f;
                    usesMaterial = true;
                }
                else if (engines)
                {
                    coverage = 1f;
                    accent = new Vector3(1f, 0.16f, 0.025f);
                }

                break;
            }
            default:
                coverage = RocketSilhouette(u, v);
                usesMaterial = coverage > 0f;
                break;
        }

        if (coverage <= 0f)
        {
            return ToPixel(background, 1f);
        }

        if (usesMaterial)
        {
            var context = new VisualEvaluationContext
            {
                Position = new Vector3(centeredX, -centeredY, normal.Z),
                Uv = new Vector2(u, v),
                Normal = normal,
                Time = 0d,
                Seed = 17,
            };
            var material = EvaluateMaterial(compilation, output, context);
            if (material is not null)
            {
                var shaded = Shade(material, normal, background);
                return ToPixel(
                    Vector3.Lerp(background, shaded, coverage),
                    (float)material.Alpha);
            }
        }

        return ToPixel(Vector3.Lerp(background, accent, coverage), 1f);
    }

    private static MaterialValue? EvaluateMaterial(
        VisualIR compilation,
        VisualIROutput output,
        VisualEvaluationContext context)
    {
        var evaluation = VisualEvaluator.Evaluate(
            compilation,
            context,
            new VisualEvaluationOptions());
        return evaluation.TryGetOutput(output.Id, out var value)
            ? value as MaterialValue
            : null;
    }

    private static double GetScalar(
        AssetRecipe asset,
        AssetInstance? instance,
        string parameterId,
        double fallback)
    {
        var parameter = asset.FindParameter(parameterId);
        var value = instance?.Resolve(parameterId) ?? parameter?.DefaultValue;
        return value is ScalarValue scalar ? scalar.Value : fallback;
    }

    private static bool NeonTube(
        float x,
        float y,
        AssetInstance? instance)
    {
        var text = instance?.Resolve("sign.text") as TextValue;
        var columns = Math.Clamp(text?.Value.Length ?? 7, 3, 12);
        var horizontal = MathF.Abs(y + 0.02f) < 0.035f &&
                         x > -0.64f &&
                         x < 0.64f;
        var vertical = MathF.Abs(x * columns) % 1f < 0.08f &&
                       MathF.Abs(y) < 0.34f;
        return horizontal || vertical;
    }

    private static Vector3 Shade(
        MaterialValue material,
        Vector3 normal,
        Vector3 environment)
    {
        var baseColor = ToVector(material.BaseColor);
        var emission = ToVector(material.EmissionColor) *
                       (float)material.EmissionStrength;
        var lightDirection = Vector3.Normalize(
            new Vector3(-0.45f, -0.5f, 0.75f));
        var viewDirection = Vector3.UnitZ;
        var halfVector = Vector3.Normalize(lightDirection + viewDirection);
        var diffuse = MathF.Max(0f, Vector3.Dot(normal, lightDirection));
        var fresnel = MathF.Pow(
            1f - MathF.Max(0f, Vector3.Dot(normal, viewDirection)),
            5f);
        var roughness = Math.Clamp((float)material.Roughness, 0.02f, 1f);
        var specularPower = 3f + MathF.Pow(1f - roughness, 3f) * 180f;
        var specular = MathF.Pow(
            MathF.Max(0f, Vector3.Dot(normal, halfVector)),
            specularPower);
        var specularColor = Vector3.Lerp(
            new Vector3(0.04f),
            baseColor,
            (float)material.Metallic);
        var ambient = baseColor * (0.13f + diffuse * 0.87f);
        var lit = ambient +
                  specularColor * (specular * (0.25f + fresnel)) +
                  new Vector3(specular * (float)material.Clearcoat);

        if (material.Transmission > 0f ||
            material.Model == VisualMaterialModel.Glass)
        {
            var transmission = Math.Clamp(
                (float)Math.Max(material.Transmission, 0.85),
                0f,
                1f);
            var glass = Vector3.Lerp(
                environment,
                Vector3.One,
                0.2f + fresnel * 0.8f);
            lit = Vector3.Lerp(lit, glass * (0.78f + diffuse * 0.3f), transmission);
        }

        var edgeGlow = fresnel * (float)material.Clearcoat;
        lit += new Vector3(edgeGlow * 0.35f);
        lit += emission * (0.5f + diffuse * 0.5f);
        return lit;
    }

    private static Vector3 StudioBackground(float u, float v)
    {
        var top = new Vector3(0.08f, 0.11f, 0.17f);
        var bottom = new Vector3(0.018f, 0.025f, 0.04f);
        var gradient = Vector3.Lerp(top, bottom, v);
        var stripe = MathF.Abs(MathF.Sin((u + v) * MathF.PI * 7f)) * 0.025f;
        return gradient + new Vector3(stripe);
    }

    private static float RocketSilhouette(float u, float v)
    {
        var x = MathF.Abs(u * 2f - 1f);
        var body = x < 0.14f && v > 0.2f && v < 0.78f;
        var nose = v <= 0.28f && x < (0.28f - v) * 1.5f;
        var fins = v > 0.67f && v < 0.83f && x < 0.36f - (v - 0.67f);
        var engine = v > 0.78f && v < 0.9f && x < 0.2f;
        return body || nose || fins || engine ? 0.92f : 0f;
    }

    private static Vector3 ToVector(ColorValue color) =>
        new(
            (float)Math.Clamp(color.Red, 0d, 1d),
            (float)Math.Clamp(color.Green, 0d, 1d),
            (float)Math.Clamp(color.Blue, 0d, 1d));

    private static VisualRgba32 ToPixel(Vector3 color, float alpha)
    {
        color = new Vector3(
            ToneMap(color.X),
            ToneMap(color.Y),
            ToneMap(color.Z));
        return new VisualRgba32(
            ToByte(color.X),
            ToByte(color.Y),
            ToByte(color.Z),
            ToByte(alpha));
    }

    private static float ToneMap(float value)
    {
        var mapped = Math.Clamp(value / (1f + MathF.Max(0f, value)), 0f, 1f);
        return MathF.Pow(mapped, 1f / 2.2f);
    }

    private static byte ToByte(float value) =>
        (byte)Math.Clamp((int)MathF.Round(value * 255f), 0, 255);
}
