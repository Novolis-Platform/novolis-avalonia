namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Lightweight preview status returned by an optional preview adapter.
/// </summary>
public sealed record AssetStudioPreviewResult(
    string Backend,
    string Status,
    bool IsStale = false,
    string? Details = null)
{
    /// <summary>Accessible summary shown in the preview panel.</summary>
    public string DisplayText
    {
        get
        {
            var stale = IsStale ? " · stale" : string.Empty;
            var details = string.IsNullOrWhiteSpace(Details)
                ? string.Empty
                : $" · {Details}";
            return $"{Backend} · {Status}{stale}{details}";
        }
    }
}
