namespace Novolis.Avalonia.Markdown;

internal static class MermaidDiagramRenderer
{
    public static string? TryRenderHtml(string mermaid, MarkdownPreviewTheme theme) =>
        Mermaid.MermaidSvg.TryRenderHtmlImage(mermaid, Map(theme));

    public static string RenderFallbackPre(string mermaid) =>
        Mermaid.MermaidSvg.RenderFallbackPre(mermaid);

    private static Mermaid.MermaidTheme Map(MarkdownPreviewTheme theme) =>
        theme switch
        {
            MarkdownPreviewTheme.GitHubLight => Mermaid.MermaidTheme.GitHubLight,
            _ => Mermaid.MermaidTheme.StudioDark,
        };
}
