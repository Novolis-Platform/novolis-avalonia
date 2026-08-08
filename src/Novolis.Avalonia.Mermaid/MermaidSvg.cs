using System.Net;
using System.Text;
using Novolis.Markup.Mermaid.Rendering;

namespace Novolis.Avalonia.Mermaid;

/// <summary>Renders Mermaid source to SVG / HTML fragments for Avalonia hosts.</summary>
public static class MermaidSvg
{
    /// <summary>Renders Mermaid source to an SVG document string, or <c>null</c> on failure.</summary>
    public static string? TryRenderSvg(string? mermaid, MermaidTheme theme = MermaidTheme.StudioDark) =>
        MermaidSvgRenderer.TryRenderSvg(mermaid, Map(theme));

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

    private static MermaidRenderTheme Map(MermaidTheme theme) =>
        theme == MermaidTheme.GitHubLight
            ? MermaidRenderTheme.GitHubLight
            : MermaidRenderTheme.StudioDark;
}
