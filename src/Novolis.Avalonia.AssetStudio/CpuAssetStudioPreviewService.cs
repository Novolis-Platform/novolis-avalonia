using Novolis.AssetStudio.Authoring;
using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;
using Novolis.Visual.Evaluation;
using Novolis.Visual.Assets;
using Novolis.Visual.Materials;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Always-available CPU reference preview for small visual recipes.
/// </summary>
public sealed class CpuAssetStudioPreviewService : IAssetStudioPreviewService
{
    private readonly VisualNodeRegistry _registry;
    private readonly AssetStudioPreviewCoordinator _coordinator;

    /// <summary>
    /// Creates a CPU preview service using the supplied node registry.
    /// </summary>
    public CpuAssetStudioPreviewService(VisualNodeRegistry? registry = null)
    {
        _registry = registry ?? VisualNodeRegistry.CreateDefault();
        _coordinator = new AssetStudioPreviewCoordinator(
            [new CpuPathTraceTarget(this)]);
    }

    /// <inheritdoc />
    public Task<AssetStudioPreviewResult> RequestAsync(
        AssetStudioSemanticState document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        return RequestAsync(
            AssetStudioDocument.Create(document.Recipe),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<AssetStudioPreviewResult> RequestAsync(
        AssetStudioDocument document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        cancellationToken.ThrowIfCancellationRequested();
        var request = new AssetStudioPreviewRequest(
            document,
            "cpu-pathtrace",
            width: 360,
            height: 260,
            shape: document.Asset is null ? "Material Sphere" : "Selected Asset",
            camera: "default-camera",
            quality: "preview",
            slotMaterialFingerprints: document.Asset?.MaterialSlots
                .Select(slot => $"{slot.Id}:{slot.MaterialId}"));
        return RequestCoreAsync(request, cancellationToken);
    }

    private async Task<AssetStudioPreviewResult> RequestCoreAsync(
        AssetStudioPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var core = await _coordinator.RequestAsync(
                request,
                cancellationToken)
            .ConfigureAwait(false);
        var details = core.Diagnostics.Length == 0
            ? null
            : string.Join(
                " · ",
                core.Diagnostics
                    .Take(2)
                    .Select(diagnostic => diagnostic.Message));
        return new AssetStudioPreviewResult(
            core.TargetId,
            core.Success ? "Ready" : "Needs attention",
            core.IsStale,
            details,
            core.Payload.ToArray());
    }

    private Novolis.AssetStudio.Authoring.AssetStudioPreviewResult Render(
        AssetStudioPreviewRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var options = new VisualCompileOptions(
            registry: _registry,
            strict: true);
        var ir = request.Compilation ?? request.Document.DocumentKind switch
        {
            AssetStudioDocumentKind.Material when request.Document.Material is { } material =>
                VisualMaterialCompiler.CompileMaterial(
                    material,
                    request.Document.MaterialInstance,
                    options),
            AssetStudioDocumentKind.Asset when request.Document.Asset is { } asset =>
                VisualMaterialCompiler.CompileAsset(
                    asset,
                    request.Document.AssetInstance,
                    options),
            _ => VisualCompiler.Compile(request.Document.Recipe, options),
        };
        var diagnostics = ir.Diagnostics
            .Select(diagnostic => AssetStudioDiagnostic.From(
                diagnostic,
                request.Document.Revision))
            .ToList();
        if (!ir.Success)
        {
            return new Novolis.AssetStudio.Authoring.AssetStudioPreviewResult(
                request.TargetId,
                request.Document.Revision,
                success: false,
                "application/json",
                diagnostics: diagnostics,
                semanticFingerprint: request.SemanticFingerprint,
                kind: AssetStudioPreviewTargetKind.PathTrace);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var evaluation = VisualEvaluator.Evaluate(
            ir,
            options: new VisualEvaluationOptions
            {
                IncludeTrace = true,
            });
        diagnostics.AddRange(
            evaluation.Diagnostics.Select(diagnostic =>
                AssetStudioDiagnostic.From(
                    diagnostic,
                    request.Document.Revision)));
        var isMaterial = ir.Outputs.Any(
            output => output.Contract.ValueKind == VisualValueKind.Material);
        byte[]? png = null;
        if (evaluation.Success)
        {
            png = request.Document.Asset is { } assembledAsset
                ? AssetStudioActualPreviewRenderer.RenderAsset(
                    ir,
                    assembledAsset,
                    request.Document.AssetInstance,
                    request.Width,
                    request.Height)
                : isMaterial
                    ? AssetStudioActualPreviewRenderer.Render(
                        ir,
                        request.Shape,
                        request.Width,
                        request.Height)
                    : AssetStudioActualPreviewRenderer.RenderGeometryPreview(
                        request.Width,
                        request.Height,
                        "Geometry output");
        }

        return new Novolis.AssetStudio.Authoring.AssetStudioPreviewResult(
            request.TargetId,
            request.Document.Revision,
            evaluation.Success,
            "image/png",
            png,
            diagnostics,
            request.SemanticFingerprint,
            AssetStudioPreviewTargetKind.PathTrace);
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

    private sealed class CpuPathTraceTarget : IAssetStudioPreviewTarget
    {
        private readonly CpuAssetStudioPreviewService _owner;

        public CpuPathTraceTarget(CpuAssetStudioPreviewService owner)
        {
            _owner = owner;
        }

        public string TargetId => "cpu-pathtrace";

        public AssetStudioPreviewTargetKind Kind =>
            AssetStudioPreviewTargetKind.PathTrace;

        public AssetStudioPreviewTargetInfo Info =>
            new(
                TargetId,
                Kind,
                VisualCapabilityStatus.Exact,
                "Deterministic CPU path tracing is available.");

        public ValueTask<
            Novolis.AssetStudio.Authoring.AssetStudioPreviewResult> PreviewAsync(
            AssetStudioPreviewRequest request,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(_owner.Render(request, cancellationToken));
    }
}
