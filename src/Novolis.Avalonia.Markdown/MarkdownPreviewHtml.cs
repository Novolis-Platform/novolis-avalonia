using Markdig;

namespace Novolis.Avalonia.Markdown;

/// <summary>Builds themed HTML documents for live preview.</summary>
public static class MarkdownPreviewHtml
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// <summary>Converts Markdown to a complete HTML document for HtmlRenderer.</summary>
    /// <param name="markdown">Markdown source.</param>
    /// <param name="theme">Visual theme.</param>
    /// <returns>Complete HTML document.</returns>
    public static string FromMarkdown(string? markdown, MarkdownPreviewTheme theme = MarkdownPreviewTheme.StudioDark)
    {
        var body = string.IsNullOrEmpty(markdown)
            ? "<p></p>"
            : Markdig.Markdown.ToHtml(markdown, Pipeline);

        var css = theme == MarkdownPreviewTheme.GitHubLight
            ? StudioMarkdownCss.GitHubLight
            : StudioMarkdownCss.Dark;

        var bodyClass = theme == MarkdownPreviewTheme.GitHubLight
            ? "markdown-body github-light"
            : "markdown-body studio";

        return $"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                <meta charset="utf-8" />
                <style>{css}</style>
                </head>
                <body class="{bodyClass}">
                {body}
                </body>
                </html>
                """;
    }
}
