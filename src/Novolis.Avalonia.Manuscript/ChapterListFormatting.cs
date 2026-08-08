using Avalonia.Controls;
using Avalonia.Media;
using Novolis.Manuscript;

namespace Novolis.Avalonia.Manuscript;

/// <summary>Shared chapter-list display helpers for manuscript editor hosts.</summary>
public static class ChapterListFormatting
{
    /// <summary>Formats a chapter list label from sort key and title.</summary>
    public static string FormatLabel(ChapterInfo chapter)
    {
        ArgumentNullException.ThrowIfNull(chapter);
        if (chapter.SortKey < 0)
            return string.IsNullOrWhiteSpace(chapter.Title) ? "Front matter" : chapter.Title;
        if (double.IsPositiveInfinity(chapter.SortKey))
            return chapter.Title ?? Path.GetFileName(chapter.FilePath);
        var key = Math.Abs(chapter.SortKey - Math.Floor(chapter.SortKey)) < 1e-9
            ? ((int)chapter.SortKey).ToString(System.Globalization.CultureInfo.InvariantCulture)
            : chapter.SortKey.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(chapter.Title) ? $"Chapter {key}" : $"{key}. {chapter.Title}";
    }

    /// <summary>Creates a simple text block for a chapter row (hosts may restyle).</summary>
    public static TextBlock CreateLabelControl(ChapterInfo chapter) =>
        new()
        {
            Text = FormatLabel(chapter),
            TextWrapping = TextWrapping.NoWrap,
        };
}
