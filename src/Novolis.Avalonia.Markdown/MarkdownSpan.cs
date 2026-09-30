using System.Text.RegularExpressions;

namespace Novolis.Avalonia.Markdown;

/// <summary>A highlight span in source text.</summary>
/// <param name="Start">Zero-based start index.</param>
/// <param name="Length">Span length.</param>
/// <param name="Kind">Highlight kind.</param>
public sealed record MarkdownSpan(int Start, int Length, MarkdownSpanKind Kind);
