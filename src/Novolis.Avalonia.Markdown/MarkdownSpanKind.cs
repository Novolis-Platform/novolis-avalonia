using System.Text.RegularExpressions;

namespace Novolis.Avalonia.Markdown;

/// <summary>Kinds of portable markdown highlight spans (mirrors editor highlighting).</summary>
public enum MarkdownSpanKind
{
    /// <summary>Double-quoted dialogue.</summary>
    Dialogue,
    /// <summary>Markdown heading line.</summary>
    Heading,
    /// <summary>Markdown or URL link.</summary>
    Link,
    /// <summary>HTML comment.</summary>
    Comment,
    /// <summary>TK / TODO / FIXME marker.</summary>
    Tk,
    /// <summary>Metadata callout line (<c>&gt; [!key]</c>).</summary>
    Metadata,
    /// <summary>Metadata key token (<c>[!key]</c>).</summary>
    MetadataKey
}
