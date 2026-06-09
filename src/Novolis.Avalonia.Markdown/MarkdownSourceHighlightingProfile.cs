namespace Novolis.Avalonia.Markdown;

/// <summary>Syntax highlighting profile for <see cref="MarkdownSourceEditor"/>.</summary>
public enum MarkdownSourceHighlightingProfile
{
    /// <summary>No syntax highlighting.</summary>
    None,

    /// <summary>Standard GitHub-flavored Markdown highlighting.</summary>
    Markdown,

    /// <summary>Book authoring: Markdown plus metadata tags and dialogue in double quotes.</summary>
    BookAuthoring,
}
