namespace Novolis.Avalonia.Briefing;

/// <summary>One line in a radio / briefing feed.</summary>
public sealed class FeedLine
{
    /// <summary>Creates a feed line.</summary>
    public FeedLine(string voice, string text, string? tag = null)
    {
        Voice = voice;
        Text = text;
        Tag = tag;
    }

    /// <summary>Voice id or channel label (e.g. <c>vox.varr</c>).</summary>
    public string Voice { get; }

    /// <summary>Spoken / displayed text.</summary>
    public string Text { get; }

    /// <summary>Optional secondary tag (day, severity).</summary>
    public string? Tag { get; }

    /// <summary>Formatted display line.</summary>
    public string Display =>
        string.IsNullOrEmpty(Tag)
            ? $"[{Voice}] {Text}"
            : $"{Tag} [{Voice}] {Text}";
}
