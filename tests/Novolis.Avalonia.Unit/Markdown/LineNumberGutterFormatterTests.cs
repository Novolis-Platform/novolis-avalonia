namespace Novolis.Avalonia.Markdown.Tests;

public class LineNumberGutterFormatterTests
{
    [Test]
    public async Task CountLines_EmptyIsOne()
    {
        await Assert.That(LineNumberGutterFormatter.CountLines(null)).IsEqualTo(1);
        await Assert.That(LineNumberGutterFormatter.CountLines("")).IsEqualTo(1);
    }

    [Test]
    public async Task CountLines_CountsNewlines()
    {
        await Assert.That(LineNumberGutterFormatter.CountLines("a\nb\nc")).IsEqualTo(3);
    }

    [Test]
    public async Task Format_HighlightsActiveLine()
    {
        var gutter = LineNumberGutterFormatter.Format(3, activeLine: 2);
        await Assert.That(gutter).Contains(">2");
        await Assert.That(gutter).Contains(" 1");
        await Assert.That(gutter).Contains(" 3");
    }
}
