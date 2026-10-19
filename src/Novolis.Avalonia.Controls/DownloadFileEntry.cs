namespace Novolis.Avalonia.Controls;

/// <summary>Metadata for one logical file or content entry in a download.</summary>
public sealed record DownloadFileEntry
{
    /// <summary>Path or display name relative to the download.</summary>
    public required string Path { get; init; }

    /// <summary>Total file size in bytes, when known.</summary>
    public long? SizeBytes { get; init; }

    /// <summary>Verified bytes available for this file.</summary>
    public long? CompletedBytes { get; init; }

    /// <summary>File progress from 0 to 100, when supplied.</summary>
    public double? ProgressPercent { get; init; }

    /// <summary>Returns normalized progress, deriving it from byte counts.</summary>
    public double? ProgressFraction
    {
        get
        {
            if (ProgressPercent is { } percent)
            {
                return Math.Clamp(percent / 100d, 0d, 1d);
            }

            if (SizeBytes is > 0 && CompletedBytes is { } completed)
            {
                return Math.Clamp((double)completed / SizeBytes.Value, 0d, 1d);
            }

            return null;
        }
    }

    /// <summary>Host-defined scheduling priority.</summary>
    public string? Priority { get; init; }

    /// <summary>Integrity state for this file.</summary>
    public string? IntegrityStatus { get; init; }
}
