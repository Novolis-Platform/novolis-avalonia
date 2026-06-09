using Markdig;

namespace Novolis.Avalonia.Markdown;

/// <summary>Builds themed HTML documents for live preview.</summary>
public static class MarkdownPreviewHtml
{
    private const double BaseFontSize = 15.0;

    /// <summary>Converts Markdown to an HTML body fragment.</summary>
    public static string ToBodyHtml(string? markdown, MarkdownPreviewTheme theme = MarkdownPreviewTheme.StudioDark) =>
        MarkdownPreviewPipeline.ToBodyHtml(markdown, theme);

    /// <summary>Converts Markdown to a complete HTML document for HtmlRenderer.</summary>
    public static string FromMarkdown(string? markdown, MarkdownPreviewTheme theme = MarkdownPreviewTheme.StudioDark) =>
        WrapDocument(ToBodyHtml(markdown, theme), theme);

    /// <summary>Wraps a body HTML fragment in a complete preview document.</summary>
    public static string WrapDocument(
        string bodyHtml,
        MarkdownPreviewTheme theme = MarkdownPreviewTheme.StudioDark,
        double fontSizePx = BaseFontSize) =>
        BuildDocument(bodyHtml, theme, fontSizePx);

    private static string BuildDocument(string bodyHtml, MarkdownPreviewTheme theme, double fontSizePx)
    {
        var css = StudioMarkdownCss.ForTheme(theme, fontSizePx);

        var bodyClass = theme == MarkdownPreviewTheme.GitHubLight
            ? "markdown-body github-light"
            : "markdown-body studio";

        var bg = theme == MarkdownPreviewTheme.GitHubLight ? "#ffffff" : "#1e1e1e";
        var fg = theme == MarkdownPreviewTheme.GitHubLight ? "#24292f" : "#e8e8e8";
        var bodyStyle = $"background-color:{bg};color:{fg};margin:0;padding:0;";

        return $"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                <meta charset="utf-8" />
                <style>{css}</style>
                </head>
                <body class="{bodyClass}" style="{bodyStyle}">
                {bodyHtml}
                </body>
                </html>
                """;
    }
}
