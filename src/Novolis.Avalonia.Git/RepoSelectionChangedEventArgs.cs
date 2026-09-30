using Novolis.IO.Git;

namespace Novolis.Avalonia.Git;

/// <summary>Repo selection changed.</summary>
public sealed class RepoSelectionChangedEventArgs : EventArgs
{
    /// <summary>Creates args.</summary>
    public RepoSelectionChangedEventArgs(RepoSelection selection) => Selection = selection;

    /// <summary>Selection.</summary>
    public RepoSelection Selection { get; }
}
