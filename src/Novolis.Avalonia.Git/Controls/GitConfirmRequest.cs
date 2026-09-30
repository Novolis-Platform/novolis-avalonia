using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Novolis.Avalonia.Git;

/// <summary>Parameters for <see cref="GitConfirmDialog"/>.</summary>
public sealed class GitConfirmRequest
{
    /// <summary>Window title.</summary>
    public required string Title { get; init; }

    /// <summary>Short summary (one or two sentences).</summary>
    public required string Summary { get; init; }

    /// <summary>Optional mono detail block.</summary>
    public string? Detail { get; init; }

    /// <summary>Risk level.</summary>
    public GitConfirmSeverity Severity { get; init; } = GitConfirmSeverity.Warning;

    /// <summary>Confirm button label.</summary>
    public string ConfirmLabel { get; init; } = "Continue";

    /// <summary>Cancel button label.</summary>
    public string CancelLabel { get; init; } = "Cancel";

    /// <summary>When set, user must type this exact phrase (case-insensitive) to enable Confirm.</summary>
    public string? RequireTypedPhrase { get; init; }

    /// <summary>Hint under the type-to-confirm box.</summary>
    public string? TypedPhraseHint { get; init; }
}
