namespace Novolis.Avalonia.Markdown.Tests;

public class MarkdownPreviewPipelineTests
{
    [Test]
    public async Task ToBodyHtml_RendersMermaidTimelineAsSvgImage()
    {
        const string markdown = """
                                ```mermaid
                                timeline
                                    title Macro timeline
                                    section Real anchor
                                        1993 : European Union forms
                                ```
                                """;

        var html = MarkdownPreviewPipeline.ToBodyHtml(markdown, MarkdownPreviewTheme.StudioDark);

        await Assert.That(html).Contains("mermaid-diagram");
        await Assert.That(html).Contains("data:image/svg+xml;base64,");
    }

    [Test]
    public async Task ToBodyHtml_RendersStandardGfm()
    {
        const string markdown = "## Title\n\nA **bold** [link](https://example.com).";

        var html = MarkdownPreviewPipeline.ToBodyHtml(markdown, MarkdownPreviewTheme.StudioDark);

        await Assert.That(html).Contains("<h2");
        await Assert.That(html).Contains("<strong>bold</strong>");
        await Assert.That(html).Contains("href=\"https://example.com\"");
    }
}
