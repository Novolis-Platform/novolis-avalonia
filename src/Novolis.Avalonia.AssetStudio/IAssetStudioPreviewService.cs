using Novolis.AssetStudio.Core;

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
}
