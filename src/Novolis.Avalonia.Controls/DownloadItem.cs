namespace Novolis.Avalonia.Controls;

/// <summary>
/// A protocol-neutral snapshot used by reusable download controls.
/// </summary>
public sealed record DownloadItem
{
    /// <summary>Display name of the content.</summary>
    public required string Name { get; init; }

    /// <summary>Total content size in bytes, when known.</summary>
    public long? TotalBytes { get; init; }

    /// <summary>Verified content written or received so far.</summary>
    public long? CompletedBytes { get; init; }

    /// <summary>Progress from 0 to 100, when supplied by the host.</summary>
    public double? ProgressPercent { get; init; }

    /// <summary>Current transfer lifecycle state.</summary>
    public DownloadTransferState State { get; init; } = DownloadTransferState.Queued;

    /// <summary>Current download rate in bytes per second.</summary>
    public long? DownloadBytesPerSecond { get; init; }

    /// <summary>Current upload or serving rate in bytes per second.</summary>
    public long? UploadBytesPerSecond { get; init; }

    /// <summary>Estimated time remaining.</summary>
    public TimeSpan? Eta { get; init; }

    /// <summary>Host-provided status detail.</summary>
    public string? StatusMessage { get; init; }

    /// <summary>Extended content, source, and output metadata.</summary>
    public DownloadMetadata Metadata { get; init; } = new();

    /// <summary>
    /// Returns progress as a normalized value, deriving it from byte counts
    /// when the host did not provide a percentage.
    /// </summary>
    public double? ProgressFraction
    {
        get
        {
            if (ProgressPercent is { } percent)
            {
                return Math.Clamp(percent / 100d, 0d, 1d);
            }

            if (TotalBytes is > 0 && CompletedBytes is { } completed)
            {
                return Math.Clamp((double)completed / TotalBytes.Value, 0d, 1d);
            }

            return null;
        }
    }
}
