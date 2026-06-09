namespace Novolis.Avalonia.Markdown.Tests;

public class MarkdownSyntaxHighlightingTests
{
    [Test]
    public async Task GetDefinition_MarkdownProfile_LoadsRules()
    {
        var definition = MarkdownSyntaxHighlighting.GetDefinition(MarkdownSourceHighlightingProfile.Markdown);

        await Assert.That(definition).IsNotNull();
        await Assert.That(definition!.Name).IsEqualTo("Novolis.Markdown");
    }

    [Test]
    public async Task GetDefinition_BookAuthoringProfile_LoadsDialogueRules()
    {
        var definition = MarkdownSyntaxHighlighting.GetDefinition(MarkdownSourceHighlightingProfile.BookAuthoring);

        await Assert.That(definition).IsNotNull();
        await Assert.That(definition!.Name).IsEqualTo("Novolis.BookAuthoring");
        await Assert.That(definition.NamedHighlightingColors.Any(c => c.Name == "Dialogue")).IsTrue();
        await Assert.That(definition.NamedHighlightingColors.Any(c => c.Name == "MetadataTag")).IsTrue();
    }
}
