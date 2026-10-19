namespace Novolis.Avalonia.Controls;

/// <summary>Actions a download surface can request from its host.</summary>
public enum DownloadCommand
{
    /// <summary>Temporarily stop transfer work.</summary>
    Pause,
    /// <summary>Start or continue transfer work.</summary>
    Resume,
    /// <summary>Retry a failed transfer.</summary>
    Retry,
    /// <summary>Remove or cancel the transfer.</summary>
    Cancel,
    /// <summary>Ask the host to verify content integrity.</summary>
    Verify,
    /// <summary>Ask the host to open the local output location.</summary>
    OpenOutput,
}
