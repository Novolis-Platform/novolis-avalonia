using Novolis.IO.Git;

namespace Novolis.Avalonia.Git;

/// <summary>Commit selected in graph.</summary>
public sealed class CommitSelectedEventArgs : EventArgs
{
    /// <summary>Creates args.</summary>
    public CommitSelectedEventArgs(CommitNode? node) => Node = node;

    /// <summary>Node or null.</summary>
    public CommitNode? Node { get; }
}
