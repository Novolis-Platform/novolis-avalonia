using System.Net;
using System.Text.RegularExpressions;
using Novolis.Markup.Markdown;

namespace Novolis.Avalonia.Markdown;

/// <summary>Studio markdown preview pipeline (Novolis Markdown + Mermaid diagrams).</summary>
public static class MarkdownPreviewPipeline
{
    private static readonly Regex PreCodeMermaidBlock = new(
        """<pre><code(?:\s+class="language-mermaid")?>([\s\S]*?)</code></pre>""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PreMermaidBlock = new(
        """<pre\s+class="mermaid">([\s\S]*?)</pre>""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Converts Markdown source to an HTML body fragment.</summary>
    public static string ToBodyHtml(string? markdown, MarkdownPreviewTheme theme = MarkdownPreviewTheme.StudioDark)
    {
        if (string.IsNullOrEmpty(markdown))
            return "<p></p>";

        var html = MarkdownToHtmlConverter.Convert(MarkdownDocument.Parse(markdown));
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
