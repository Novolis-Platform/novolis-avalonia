using Novolis.Markup.Markdown.Mermaid.Rendering;
using Novolis.Markup.Mermaid.Rendering;

namespace Novolis.Avalonia.Markdown;

/// <summary>Studio markdown preview pipeline (Novolis Markdown + Mermaid diagrams).</summary>
public static class MarkdownPreviewPipeline
{
    /// <summary>Converts Markdown source to an HTML body fragment.</summary>
    public static string ToBodyHtml(string? markdown, MarkdownPreviewTheme theme = MarkdownPreviewTheme.StudioDark) =>
        MermaidMarkdownHtmlRenderer.ToHtml(markdown ?? string.Empty, Map(theme));

    private static MermaidRenderTheme Map(MarkdownPreviewTheme theme) =>
        theme == MarkdownPreviewTheme.GitHubLight
            ? MermaidRenderTheme.GitHubLight
            : MermaidRenderTheme.StudioDark;
}
