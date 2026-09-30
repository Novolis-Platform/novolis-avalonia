using Novolis.IO.Git;

namespace Novolis.Avalonia.Git;

/// <summary>Event args for chrome commands.</summary>
public sealed class GitChromeCommandEventArgs : EventArgs
{
    /// <summary>Creates args.</summary>
    public GitChromeCommandEventArgs(
        GitChromeCommand command,
        string? repoPath = null,
        int? stashIndex = null,
        string? detail = null)
    {
        Command = command;
        RepoPath = repoPath;
        StashIndex = stashIndex;
        Detail = detail;
    }

    /// <summary>Command.</summary>
    public GitChromeCommand Command { get; }

    /// <summary>Optional focused repo path.</summary>
    public string? RepoPath { get; }

    /// <summary>Optional stash index.</summary>
    public int? StashIndex { get; }

    /// <summary>Optional human detail (e.g. stash message).</summary>
    public string? Detail { get; }
}
