using System.Text.RegularExpressions;

namespace Novolis.Avalonia.Markdown;

/// <summary>Options for <see cref="MarkdownSpanAnalyzer"/>.</summary>
public sealed class MarkdownSpanOptions
{
    /// <summary>Highlight headings.</summary>
    public bool Headings { get; init; } = true;

    /// <summary>Highlight links.</summary>
    public bool Links { get; init; } = true;

    /// <summary>Highlight HTML comments.</summary>
    public bool Comments { get; init; } = true;

    /// <summary>Highlight TK/TODO/FIXME.</summary>
    public bool Tk { get; init; } = true;

    /// <summary>Highlight double-quoted dialogue.</summary>
    public bool Dialogue { get; init; } = true;

    /// <summary>Highlight metadata callouts.</summary>
    public bool Metadata { get; init; } = true;
}
