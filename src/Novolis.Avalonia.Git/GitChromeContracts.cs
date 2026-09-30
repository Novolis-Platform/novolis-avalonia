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
