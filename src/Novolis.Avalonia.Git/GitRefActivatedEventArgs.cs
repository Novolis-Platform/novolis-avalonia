using Novolis.IO.Git;

namespace Novolis.Avalonia.Git;

/// <summary>Branch/ref activation.</summary>
public sealed class GitRefActivatedEventArgs : EventArgs
{
    /// <summary>Creates args.</summary>
    public GitRefActivatedEventArgs(TipRef tip) => Tip = tip;

    /// <summary>Tip.</summary>
    public TipRef Tip { get; }
}
