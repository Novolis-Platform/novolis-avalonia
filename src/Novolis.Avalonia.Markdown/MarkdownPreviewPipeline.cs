using System.Net;
using System.Text.RegularExpressions;
using Markdig;

namespace Novolis.Avalonia.Markdown;

/// <summary>Shared Markdig pipeline for studio markdown preview (GFM + Mermaid diagrams).</summary>
public static class MarkdownPreviewPipeline
{
    private static readonly MarkdownPipeline GfmPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private static readonly Regex PreCodeMermaidBlock = new(
        """<pre><code(?:\s+class="language-mermaid")?>([\s\S]*?)</code></pre>""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PreMermaidBlock = new(
        """<pre\s+class="mermaid">([\s\S]*?)</pre>""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Converts Markdown source to an HTML body fragment.</summary>
    /// <param name="markdown">Markdown source.</param>
    /// <param name="theme">Preview theme used for diagram colors.</param>
    /// <returns>HTML fragment suitable for wrapping in a preview document.</returns>
    public static string ToBodyHtml(string? markdown, MarkdownPreviewTheme theme = MarkdownPreviewTheme.StudioDark)
    {
        if (string.IsNullOrEmpty(markdown))
            return "<p></p>";

        var html = Markdig.Markdown.ToHtml(markdown, GfmPipeline);
        html = ReplaceMermaidBlocks(html, PreCodeMermaidBlock, theme);
        html = ReplaceMermaidBlocks(html, PreMermaidBlock, theme);
        return html;
    }

    private static string ReplaceMermaidBlocks(string html, Regex pattern, MarkdownPreviewTheme theme) =>
        pattern.Replace(html, match =>
        {
            var mermaid = WebUtility.HtmlDecode(match.Groups[1].Value);
            return MermaidDiagramRenderer.TryRenderHtml(mermaid, theme)
                ?? MermaidDiagramRenderer.RenderFallbackPre(mermaid);
        });
}
