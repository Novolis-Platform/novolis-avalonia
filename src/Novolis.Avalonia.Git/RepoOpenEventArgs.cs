using Novolis.IO.Git;

namespace Novolis.Avalonia.Git;

/// <summary>Open-repo gesture.</summary>
public sealed class RepoOpenEventArgs : EventArgs
{
    /// <summary>Creates args.</summary>
    public RepoOpenEventArgs(RepoEntry repo) => Repo = repo;

    /// <summary>Repo.</summary>
    public RepoEntry Repo { get; }
}
