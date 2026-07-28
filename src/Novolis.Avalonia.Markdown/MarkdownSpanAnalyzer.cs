using System.Text.RegularExpressions;

namespace Novolis.Avalonia.Markdown;

/// <summary>Kinds of portable markdown highlight spans (mirrors editor highlighting).</summary>
public enum MarkdownSpanKind
{
    /// <summary>Double-quoted dialogue.</summary>
    Dialogue,
    /// <summary>Markdown heading line.</summary>
    Heading,
    /// <summary>Markdown or URL link.</summary>
    Link,
    /// <summary>HTML comment.</summary>
    Comment,
    /// <summary>TK / TODO / FIXME marker.</summary>
    Tk,
    /// <summary>Metadata callout line (<c>&gt; [!key]</c>).</summary>
    Metadata,
    /// <summary>Metadata key token (<c>[!key]</c>).</summary>
    MetadataKey
}

/// <summary>A highlight span in source text.</summary>
/// <param name="Start">Zero-based start index.</param>
/// <param name="Length">Span length.</param>
/// <param name="Kind">Highlight kind.</param>
public sealed record MarkdownSpan(int Start, int Length, MarkdownSpanKind Kind);

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

/// <summary>Portable span analyzer for book-authoring markdown (no AvaloniaEdit dependency).</summary>
public static class MarkdownSpanAnalyzer
{
    static readonly Regex HeadingRegex = new(@"^#{1,6}\s.+$", RegexOptions.Multiline | RegexOptions.Compiled);
    static readonly Regex LinkRegex = new(@"\[[^\]]+\]\([^)]+\)|https?://[^\s)]+", RegexOptions.Compiled);
    static readonly Regex CommentRegex = new(@"<!--.*?-->", RegexOptions.Singleline | RegexOptions.Compiled);
    static readonly Regex TkRegex = new(@"\b(TK|TODO|FIXME)\b", RegexOptions.Compiled);
    static readonly Regex DialogueRegex = new(@"""[^""\r\n]*""", RegexOptions.Compiled);
    static readonly Regex MetadataLineRegex = new(@"^>\s*\[![A-Za-z0-9_-]+\].*$", RegexOptions.Multiline | RegexOptions.Compiled);
    static readonly Regex MetadataKeyRegex = new(@"^>\s*(\[![A-Za-z0-9_-]+\])", RegexOptions.Multiline | RegexOptions.Compiled);
    static readonly Regex CalloutPrefix = new(@"^\s*>\s*\[!", RegexOptions.Compiled);

    /// <summary>Analyzes <paramref name="text"/> and returns ordered highlight spans.</summary>
    public static IReadOnlyList<MarkdownSpan> Analyze(string text, MarkdownSpanOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        options ??= new MarkdownSpanOptions();
        var spans = new List<MarkdownSpan>();

        if (options.Headings)
            AddMatches(spans, text, HeadingRegex, MarkdownSpanKind.Heading);
        if (options.Metadata)
            AddMetadata(spans, text);
        if (options.Links)
            AddMatches(spans, text, LinkRegex, MarkdownSpanKind.Link);
        if (options.Comments)
            AddMatches(spans, text, CommentRegex, MarkdownSpanKind.Comment);
        if (options.Tk)
            AddMatches(spans, text, TkRegex, MarkdownSpanKind.Tk);
        if (options.Dialogue)
            AddDialogue(spans, text);

        return spans.OrderBy(s => s.Start).ThenByDescending(s => s.Length).ToList();
    }

    static void AddMetadata(List<MarkdownSpan> spans, string text)
    {
        foreach (Match m in MetadataLineRegex.Matches(text))
            spans.Add(new MarkdownSpan(m.Index, m.Length, MarkdownSpanKind.Metadata));
        foreach (Match m in MetadataKeyRegex.Matches(text))
        {
            if (m.Groups.Count > 1)
                spans.Add(new MarkdownSpan(m.Groups[1].Index, m.Groups[1].Length, MarkdownSpanKind.MetadataKey));
        }
    }

    static void AddDialogue(List<MarkdownSpan> spans, string text)
    {
        foreach (Match m in DialogueRegex.Matches(text))
        {
            if (IsInsideMetadataLine(text, m.Index))
                continue;
            spans.Add(new MarkdownSpan(m.Index, m.Length, MarkdownSpanKind.Dialogue));
        }
    }

    static bool IsInsideMetadataLine(string text, int index)
    {
        var lineStart = text.LastIndexOf('\n', Math.Max(0, index - 1)) + 1;
        var lineEnd = text.IndexOf('\n', index);
        if (lineEnd < 0)
            lineEnd = text.Length;
        var line = text[lineStart..lineEnd];
        return CalloutPrefix.IsMatch(line);
    }

    static void AddMatches(List<MarkdownSpan> spans, string text, Regex regex, MarkdownSpanKind kind)
    {
        foreach (Match m in regex.Matches(text))
            spans.Add(new MarkdownSpan(m.Index, m.Length, kind));
    }
}
