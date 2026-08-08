using Novolis.IO.Git;

namespace Novolis.Avalonia.Git;

/// <summary>Git action requested from chrome (host performs I/O).</summary>
public enum GitChromeCommand
{
    /// <summary>Refresh status / graph.</summary>
    Refresh,

    /// <summary>Fetch.</summary>
    Fetch,

    /// <summary>Pull ff-only.</summary>
    Pull,

    /// <summary>Push.</summary>
    Push,

    /// <summary>Create branch dialog.</summary>
    CreateBranch,

    /// <summary>Multi-repo branch cut.</summary>
    BranchCut,

    /// <summary>Stash push.</summary>
    StashPush,

    /// <summary>Stash apply.</summary>
    StashApply,

    /// <summary>Stash pop.</summary>
    StashPop,

    /// <summary>Stash drop.</summary>
    StashDrop,
}

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

/// <summary>Repo selection changed.</summary>
public sealed class RepoSelectionChangedEventArgs : EventArgs
{
    /// <summary>Creates args.</summary>
    public RepoSelectionChangedEventArgs(RepoSelection selection) => Selection = selection;

    /// <summary>Selection.</summary>
    public RepoSelection Selection { get; }
}

/// <summary>Open-repo gesture.</summary>
public sealed class RepoOpenEventArgs : EventArgs
{
    /// <summary>Creates args.</summary>
    public RepoOpenEventArgs(RepoEntry repo) => Repo = repo;

    /// <summary>Repo.</summary>
    public RepoEntry Repo { get; }
}

/// <summary>Commit selected in graph.</summary>
public sealed class CommitSelectedEventArgs : EventArgs
{
    /// <summary>Creates args.</summary>
    public CommitSelectedEventArgs(CommitNode? node) => Node = node;

    /// <summary>Node or null.</summary>
    public CommitNode? Node { get; }
}

/// <summary>Branch/ref activation.</summary>
public sealed class GitRefActivatedEventArgs : EventArgs
{
    /// <summary>Creates args.</summary>
    public GitRefActivatedEventArgs(TipRef tip) => Tip = tip;

    /// <summary>Tip.</summary>
    public TipRef Tip { get; }
}
