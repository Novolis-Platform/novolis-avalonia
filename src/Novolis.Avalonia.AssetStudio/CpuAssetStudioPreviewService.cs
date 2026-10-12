using System.Collections.Concurrent;
using Novolis.AssetStudio.Core;
using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;
using Novolis.Visual.Evaluation;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Always-available CPU reference preview for small visual recipes.
/// </summary>
public sealed class CpuAssetStudioPreviewService : IAssetStudioPreviewService
{
    private readonly VisualNodeRegistry _registry;
    private readonly ConcurrentDictionary<string, AssetStudioPreviewResult> _cache =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Creates a CPU preview service using the supplied node registry.
    /// </summary>
    public CpuAssetStudioPreviewService(VisualNodeRegistry? registry = null)
    {
        _registry = registry ?? VisualNodeRegistry.CreateDefault();
    }

    /// <inheritdoc />
    public Task<AssetStudioPreviewResult> RequestAsync(
        AssetStudioSemanticState document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        cancellationToken.ThrowIfCancellationRequested();
        var cacheKey = document.Fingerprint + "|cpu|192x128|surface";
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            return Task.FromResult(cached);
        }

        var ir = VisualCompiler.Compile(
            document.Recipe,
            new VisualCompileOptions(
                registry: _registry,
                strict: true));
        if (!ir.Success)
        {
            var failed = new AssetStudioPreviewResult(
                    "CPU reference",
                    "Needs attention",
                    Details: FormatDiagnostics(ir.Diagnostics));
            return Task.FromResult(failed);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var evaluation = VisualEvaluator.Evaluate(
            ir,
            options: new VisualEvaluationOptions
            {
                IncludeTrace = true,
            });
        var isMaterial = ir.Outputs.Any(
            output => output.Contract.ValueKind == VisualValueKind.Material);
        var png = evaluation.Success
            ? isMaterial
                ? AssetStudioMaterialPreviewRenderer.Render(
                    ir,
                    "Material Sphere",
                    width: 360,
                    height: 260)
                : AssetStudioMaterialPreviewRenderer.RenderStudioPlaceholder(
                    width: 360,
                    height: 260,
                    label: "Geometry output")
            : null;
        var details = evaluation.Success
            ? $"{FormatOutputs(evaluation.Outputs)} · {evaluation.Trace.Length} node(s) evaluated"
            : FormatDiagnostics(evaluation.Diagnostics);
        var result = new AssetStudioPreviewResult(
                "CPU reference",
                evaluation.Success ? "Ready" : "Needs attention",
                Details: details,
                PngBytes: png);
        if (evaluation.Success)
        {
            _cache[cacheKey] = result;
            while (_cache.Count > 32 &&
                   _cache.Keys.Order(StringComparer.Ordinal).FirstOrDefault()
                       is { } oldest)
            {
                _cache.TryRemove(oldest, out _);
            }
        }

        return Task.FromResult(result);
    }

    private static string FormatOutputs(
        IReadOnlyDictionary<string, VisualValue> outputs)
    {
        if (outputs.Count == 0)
        {
            return "no outputs";
        }

        return string.Join(
            " · ",
            outputs.Select(output => $"{output.Key} = {FormatValue(output.Value)}"));
    }

    private static string FormatValue(VisualValue value) =>
        value switch
        {
            ScalarValue scalar =>
                scalar.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
            IntegerValue integer => integer.Value.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            BooleanValue boolean => boolean.Value ? "true" : "false",
            TextValue text => text.Value,
            ColorValue color =>
                $"rgba({color.Red:0.##}, {color.Green:0.##}, {color.Blue:0.##}, {color.Alpha:0.##})",
            VectorValue vector => $"({string.Join(", ", vector.Components.Select(
                component => component.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)))})",
            _ => value.Kind.ToString(),
        };

    private static string FormatDiagnostics(
        IEnumerable<VisualDiagnostic> diagnostics)
    {
        var values = diagnostics
            .Take(2)
            .Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")
            .ToArray();
        return values.Length == 0
            ? "no diagnostic details"
            : string.Join(" · ", values);
    }
}
