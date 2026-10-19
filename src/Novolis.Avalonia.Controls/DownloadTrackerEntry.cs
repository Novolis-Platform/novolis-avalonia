namespace Novolis.Avalonia.Controls;

/// <summary>Metadata for one tracker or source-discovery endpoint.</summary>
public sealed record DownloadTrackerEntry
{
    /// <summary>Tracker endpoint or host-provided discovery identifier.</summary>
    public required string Endpoint { get; init; }

    /// <summary>Protocol label for the endpoint.</summary>
    public string? Protocol { get; init; }

    /// <summary>Current announce or discovery state.</summary>
    public string? State { get; init; }

    /// <summary>Complete sources reported by this endpoint.</summary>
    public int? SeedCount { get; init; }

    /// <summary>Incomplete sources reported by this endpoint.</summary>
    public int? PeerCount { get; init; }

    /// <summary>Most recent successful or attempted contact.</summary>
    public DateTimeOffset? LastContactUtc { get; init; }

    /// <summary>Most recent endpoint error.</summary>
    public string? Error { get; init; }
}
