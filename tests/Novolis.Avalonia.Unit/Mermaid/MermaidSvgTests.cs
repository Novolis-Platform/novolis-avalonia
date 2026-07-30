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
}
