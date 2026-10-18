using Novolis.AssetStudio.Authoring;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Optional preview adapter boundary. Implementations may use CPU or GPU
/// services, while the editor remains usable when no adapter is registered.
/// </summary>
public interface IAssetStudioPreviewService
{
    /// <summary>Requests a preview for the current semantic document.</summary>
    Task<AssetStudioPreviewResult> RequestAsync(
        AssetStudioSemanticState document,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests a preview while preserving material or asset document context.
    /// Existing adapters that only understand semantic recipes receive the
    /// compatible projection.
    /// </summary>
    Task<AssetStudioPreviewResult> RequestAsync(
        AssetStudioDocument document,
        CancellationToken cancellationToken = default) =>
        RequestAsync(document.Semantic, cancellationToken);
}
