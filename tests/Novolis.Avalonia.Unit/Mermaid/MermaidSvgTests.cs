namespace Novolis.Avalonia.Mermaid.Tests;

public class MermaidSvgTests
{
    [Test]
    public async Task TryRenderSvg_SequenceDiagram_ReturnsSvg()
    {
        const string source = """
                              sequenceDiagram
                                participant A as Alice
                                participant B as Bob
                                A->>B: Hello
                              """;

        var svg = MermaidSvg.TryRenderSvg(source, MermaidTheme.StudioDark);

        await Assert.That(svg).IsNotNull();
        await Assert.That(svg!).Contains("<svg");
    }

    [Test]
    public async Task TryRenderHtmlImage_WrapsBase64Img()
    {
        const string source = """
                              flowchart TD
                                A[Start] --> B[Done]
                              """;

        var html = MermaidSvg.TryRenderHtmlImage(source, MermaidTheme.GitHubLight);

        await Assert.That(html).IsNotNull();
        await Assert.That(html!).Contains("mermaid-diagram");
        await Assert.That(html!).Contains("data:image/svg+xml;base64,");
    }

    [Test]
    public async Task MermaidControl_RendersSourceToSvg()
    {
        var control = new MermaidControl
        {
            Source = """
                     pie title Pets
                     "Dogs" : 40
                     "Cats" : 60
                     """,
        };
        control.Refresh();

        await Assert.That(control.Svg).IsNotNull();
        await Assert.That(control.Svg!).Contains("<svg");
    }

    [Test]
    public async Task TryRenderSvg_NullOrBlank_ReturnsNull()
    {
        await Assert.That(MermaidSvg.TryRenderSvg(null)).IsNull();
        await Assert.That(MermaidSvg.TryRenderSvg("   ")).IsNull();
    }

    [Test]
    public async Task TryRenderSvg_InvalidSource_ReturnsNull()
    {
        await Assert.That(MermaidSvg.TryRenderSvg("this is not mermaid")).IsNull();
    }

    [Test]
    public async Task RenderFallbackPre_EscapesHtml()
    {
        var html = MermaidSvg.RenderFallbackPre("<script>alert(1)</script>");
        await Assert.That(html.Contains("<script>", StringComparison.Ordinal)).IsFalse();
        await Assert.That(html.Contains("mermaid-source", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task TryRenderSvg_GitHubLight_ReturnsSvg()
    {
        const string source = """
                              flowchart TD
                                A --> B
                              """;

        var svg = MermaidSvg.TryRenderSvg(source, MermaidTheme.GitHubLight);
        await Assert.That(svg).IsNotNull();
        await Assert.That(svg!).Contains("<svg");
    }
}
