using System.Xml;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

namespace Novolis.Avalonia.Markdown;

/// <summary>Loads Novolis Markdown syntax highlighting definitions for AvaloniaEdit.</summary>
public static class MarkdownSyntaxHighlighting
{
    private static IHighlightingDefinition? _markdown;
    private static IHighlightingDefinition? _bookAuthoring;

    /// <summary>Returns the highlighting definition for the given profile.</summary>
    public static IHighlightingDefinition? GetDefinition(MarkdownSourceHighlightingProfile profile) =>
        profile switch
        {
            MarkdownSourceHighlightingProfile.Markdown => _markdown ??= Load("Novolis.Avalonia.Markdown.Highlighting.MarkdownStudio.xshd"),
            MarkdownSourceHighlightingProfile.BookAuthoring => _bookAuthoring ??= Load("Novolis.Avalonia.Markdown.Highlighting.BookAuthoringStudio.xshd"),
            _ => null,
        };

    private static IHighlightingDefinition Load(string resourceName)
    {
        var assembly = typeof(MarkdownSyntaxHighlighting).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded highlighting resource '{resourceName}'.");

        using var reader = XmlReader.Create(stream);
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}
