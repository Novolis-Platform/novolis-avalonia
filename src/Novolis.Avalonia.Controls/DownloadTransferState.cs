namespace Novolis.Avalonia.Controls;

/// <summary>Generic lifecycle state for a queued download.</summary>
public enum DownloadTransferState
{
    /// <summary>Waiting for the host to start transfer work.</summary>
    Queued,
    /// <summary>Validating existing content or received pieces.</summary>
    Checking,
    /// <summary>Receiving content from one or more sources.</summary>
    Downloading,
    /// <summary>Temporarily paused by the host or user.</summary>
    Paused,
    /// <summary>Serving completed content to other sources.</summary>
    Seeding,
    /// <summary>Content is complete and verified.</summary>
    Completed,
    /// <summary>No usable source is currently transferring.</summary>
    Stalled,
    /// <summary>The transfer stopped because of an error.</summary>
    Failed,
    /// <summary>The item was removed from the active queue.</summary>
    Removed,
}
