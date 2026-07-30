using System.Net;
using System.Text;
using Mermaider;
using Mermaider.Models;

namespace Novolis.Avalonia.Mermaid;

/// <summary>Renders Mermaid source to SVG / HTML fragments using Mermaider (no browser).</summary>
public static class MermaidSvg
{
    /// <summary>Renders Mermaid source to an SVG document string, or <c>null</c> on failure.</summary>
    public static string? TryRenderSvg(string? mermaid, MermaidTheme theme = MermaidTheme.StudioDark)
    {
        if (string.IsNullOrWhiteSpace(mermaid))
            return null;

        try
        {
            var svg = MermaidRenderer.RenderSvg(mermaid.Trim(), OptionsFor(theme));
            return string.IsNullOrWhiteSpace(svg) ? null : svg;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Wraps a rendered SVG as a base64 <c>&lt;img&gt;</c> HTML fragment for HtmlPanel hosts.</summary>
    public static string? TryRenderHtmlImage(string? mermaid, MermaidTheme theme = MermaidTheme.StudioDark)
    {
        var svg = TryRenderSvg(mermaid, theme);
        if (svg is null)
            return null;

        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
        return $"""
               <div class="mermaid-diagram">
               <img alt="Mermaid diagram" src="data:image/svg+xml;base64,{base64}" />
               </div>
               """;
    }

    /// <summary>Fallback HTML when rendering fails.</summary>
    public static string RenderFallbackPre(string mermaid) =>
        $"""
         <pre class="mermaid-source"><code>{WebUtility.HtmlEncode(mermaid)}</code></pre>
         """;

    /// <summary>Maps a theme to Mermaider render options.</summary>
    public static RenderOptions OptionsFor(MermaidTheme theme) =>
        theme switch
        {
            MermaidTheme.GitHubLight => new RenderOptions
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
