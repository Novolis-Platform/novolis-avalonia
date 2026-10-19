namespace Novolis.Avalonia.Controls;

/// <summary>
/// Protocol-neutral snapshot of one peer or source contributing to a
/// download.
/// </summary>
public sealed record DownloadPeer
{
    /// <summary>Peer endpoint or host-provided source identifier.</summary>
    public required string Endpoint { get; init; }

    /// <summary>Country, region, network, or other location label.</summary>
    public string? Location { get; init; }

    /// <summary>Remote client or source implementation label.</summary>
    public string? Client { get; init; }

    /// <summary>Host-defined connection direction or role.</summary>
    public string? Direction { get; init; }

    /// <summary>Remote completion percentage for the content.</summary>
    public double? CompletionPercent { get; init; }

    /// <summary>Current download rate from this peer in bytes per second.</summary>
    public long? DownloadBytesPerSecond { get; init; }

    /// <summary>Current upload rate to this peer in bytes per second.</summary>
    public long? UploadBytesPerSecond { get; init; }

    /// <summary>Piece availability bitmap, when the source exposes one.</summary>
    public IReadOnlyList<bool> PieceAvailability { get; init; } = [];

    /// <summary>Total bytes received from this peer.</summary>
    public long? DownloadedBytes { get; init; }

    /// <summary>Total bytes sent to this peer.</summary>
    public long? UploadedBytes { get; init; }

    /// <summary>Connection state or host-provided diagnostic.</summary>
    public string? ConnectionState { get; init; }
}
