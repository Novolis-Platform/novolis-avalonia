using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Novolis.Avalonia.Git;

/// <summary>Severity for destructive / caution confirms.</summary>
public enum GitConfirmSeverity
{
    /// <summary>Informational confirm (batch pull, push).</summary>
    Info,

    /// <summary>Caution (pop, dirty checkout).</summary>
    Warning,

    /// <summary>Irreversible / high risk (stash drop, branch-cut apply).</summary>
    Danger,
}
