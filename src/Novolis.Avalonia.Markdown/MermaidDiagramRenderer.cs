using System.Net;
using System.Text;
using Mermaider;
using Mermaider.Models;

namespace Novolis.Avalonia.Markdown;

internal static class MermaidDiagramRenderer
{
    public static string? TryRenderHtml(string mermaid, MarkdownPreviewTheme theme)
    {
        if (string.IsNullOrWhiteSpace(mermaid))
            return null;

        try
        {
            var svg = MermaidRenderer.RenderSvg(mermaid.Trim(), OptionsFor(theme));
            if (string.IsNullOrWhiteSpace(svg))
                return null;

            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
            return $"""
                    <div class="mermaid-diagram">
                    <img alt="Mermaid diagram" src="data:image/svg+xml;base64,{base64}" />
                    </div>
                    """;
        }
        catch
        {
            return null;
        }
    }

    public static string RenderFallbackPre(string mermaid) =>
        $"""
         <pre class="mermaid-source"><code>{WebUtility.HtmlEncode(mermaid)}</code></pre>
         """;

    private static RenderOptions OptionsFor(MarkdownPreviewTheme theme) =>
        theme switch
        {
            MarkdownPreviewTheme.GitHubLight => new RenderOptions
            {
                Bg = "#ffffff",
                Fg = "#24292f",
                Line = "#d8dee4",
                Accent = "#0969da",
                Muted = "#57606a",
                Surface = "#f6f8fa",
                Border = "#d8dee4",
                Font = "Segoe UI, system-ui, sans-serif",
                FontSize = "14px",
            },
            _ => new RenderOptions
            {
                Bg = "#1e1e1e",
                Fg = "#e8e8e8",
                Line = "#555555",
                Accent = "#6eb5ff",
                Muted = "#9da5ae",
                Surface = "#252526",
                Border = "#3a3a3a",
                Font = "Segoe UI, system-ui, sans-serif",
                FontSize = "14px",
            },
        };
}
