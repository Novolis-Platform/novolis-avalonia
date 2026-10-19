namespace Novolis.Avalonia.Controls;

/// <summary>
/// Protocol-neutral metadata associated with a downloadable item.
/// </summary>
public sealed record DownloadMetadata
{
    /// <summary>Media or document type, when known.</summary>
    public string? ContentType { get; init; }

    /// <summary>Stable content hash or infohash, when available.</summary>
    public string? Hash { get; init; }

    /// <summary>Other names observed for the same content hash.</summary>
    public IReadOnlyList<string> AlternateNames { get; init; } = [];

    /// <summary>Estimated swarm availability as a percentage.</summary>
    public double? AvailabilityPercent { get; init; }

    /// <summary>Total known source count.</summary>
    public int? SourceCount { get; init; }

    /// <summary>Known complete sources.</summary>
    public int? SeedCount { get; init; }

    /// <summary>Known incomplete or transferring peers.</summary>
    public int? PeerCount { get; init; }

    /// <summary>Transport or protocol label supplied by the host.</summary>
    public string? Protocol { get; init; }

    /// <summary>Logical transfer source or session path.</summary>
    public string? TransferPath { get; init; }

    /// <summary>Local path where the completed content is written.</summary>
    public string? OutputLocation { get; init; }

    /// <summary>Number of pieces or chunks, when the protocol exposes it.</summary>
    public int? PieceCount { get; init; }

    /// <summary>Piece or chunk size in bytes, when known.</summary>
    public long? PieceSizeBytes { get; init; }

    /// <summary>Known source addresses or source labels.</summary>
    public IReadOnlyList<string> Sources { get; init; } = [];

    /// <summary>Detailed peer snapshots for the selected content.</summary>
    public IReadOnlyList<DownloadPeer> Peers { get; init; } = [];

    /// <summary>Logical files or content entries contained in the item.</summary>
    public IReadOnlyList<DownloadFileEntry> Files { get; init; } = [];

    /// <summary>Tracker or source-discovery endpoint metadata.</summary>
    public IReadOnlyList<DownloadTrackerEntry> Trackers { get; init; } = [];

    /// <summary>Time at which the item entered the queue.</summary>
    public DateTimeOffset? AddedAtUtc { get; init; }

    /// <summary>Time at which the item last completed successfully.</summary>
    public DateTimeOffset? CompletedAtUtc { get; init; }

    /// <summary>Integrity or verification state supplied by the host.</summary>
    public string? VerificationStatus { get; init; }

    /// <summary>Most recent non-fatal or fatal error, when available.</summary>
    public string? Error { get; init; }
}
