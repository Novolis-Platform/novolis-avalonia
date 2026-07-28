namespace Novolis.Avalonia.Markdown.Tests;

public sealed class MarkdownSpanAnalyzerTests
{
    [Test]
    public async Task Analyze_Dialogue_Metadata_And_Tk()
    {
        var text = """
            # Chapter 1 - Test

            > [!pov] Ryn

            She said "Hello there" and marked TK for later.
            <!-- note -->
            See [docs](https://example.com).
            """;

        var spans = MarkdownSpanAnalyzer.Analyze(text);
        await Assert.That(spans.Any(s => s.Kind == MarkdownSpanKind.Heading)).IsTrue();
        await Assert.That(spans.Any(s => s.Kind == MarkdownSpanKind.Metadata)).IsTrue();
        await Assert.That(spans.Any(s => s.Kind == MarkdownSpanKind.MetadataKey)).IsTrue();
        await Assert.That(spans.Any(s => s.Kind == MarkdownSpanKind.Dialogue)).IsTrue();
        await Assert.That(spans.Any(s => s.Kind == MarkdownSpanKind.Tk)).IsTrue();
        await Assert.That(spans.Any(s => s.Kind == MarkdownSpanKind.Comment)).IsTrue();
        await Assert.That(spans.Any(s => s.Kind == MarkdownSpanKind.Link)).IsTrue();
    }

    [Test]
    public async Task Dialogue_Skips_Quotes_Inside_Metadata()
    {
        var text = "> [!notes] said \"ignore me\"\n\nBody \"keep me\"\n";
        var spans = MarkdownSpanAnalyzer.Analyze(text, new MarkdownSpanOptions
        {
            Headings = false,
            Links = false,
            Comments = false,
            Tk = false,
            Dialogue = true,
            Metadata = true
        });
        var dialogue = spans.Where(s => s.Kind == MarkdownSpanKind.Dialogue).ToList();
        await Assert.That(dialogue.Count).IsEqualTo(1);
        await Assert.That(text.Substring(dialogue[0].Start, dialogue[0].Length)).IsEqualTo("\"keep me\"");
    }
}
