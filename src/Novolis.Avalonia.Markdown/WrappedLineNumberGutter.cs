using System.Text;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Novolis.Avalonia.Markdown;

/// <summary>Computes wrap-aware gutter rows that align with a word-wrapping text editor.</summary>
internal static class WrappedLineNumberGutter
{
    public static string Build(
        string? text,
        Typeface typeface,
        double fontSize,
        double contentWidth,
        bool wordWrap,
        int activeLogicalLine)
    {
        var logicalLineCount = LineNumberGutterFormatter.CountLines(text);
        if (!wordWrap || contentWidth <= 1 || string.IsNullOrEmpty(text))
            return LineNumberGutterFormatter.Format(logicalLineCount, activeLogicalLine);

        var visualLinesPerLogical = CountVisualLinesPerLogicalLine(text, typeface, fontSize, contentWidth);
        return LineNumberGutterFormatter.FormatWrapped(visualLinesPerLogical, activeLogicalLine);
    }

    public static int[] CountVisualLinesPerLogicalLine(
        string text,
        Typeface typeface,
        double fontSize,
        double contentWidth)
    {
        var logicalLines = SplitLogicalLines(text);
        var counts = new int[logicalLines.Count];

        using var layout = new TextLayout(
            text,
            typeface,
            fontSize,
            textWrapping: TextWrapping.Wrap,
            maxWidth: contentWidth);

        if (layout.TextLines.Count == 0)
        {
            Array.Fill(counts, 1);
            return counts;
        }

        for (var i = 0; i < logicalLines.Count; i++)
            counts[i] = 0;

        foreach (var textLine in layout.TextLines)
        {
            var logicalIndex = LogicalLineIndex(logicalLines, textLine.FirstTextSourceIndex);
            counts[logicalIndex]++;
        }

        for (var i = 0; i < counts.Length; i++)
        {
            if (counts[i] == 0)
                counts[i] = 1;
        }

        return counts;
    }

    private static int LogicalLineIndex(IReadOnlyList<LogicalLineSpan> lines, int charIndex)
    {
        for (var i = lines.Count - 1; i >= 0; i--)
        {
            if (charIndex >= lines[i].Start)
                return i;
        }

        return 0;
    }

    private static List<LogicalLineSpan> SplitLogicalLines(string text)
    {
        var lines = new List<LogicalLineSpan>();
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '\n')
                continue;

            lines.Add(new LogicalLineSpan(start, i - start));
            start = i + 1;
        }

        lines.Add(new LogicalLineSpan(start, text.Length - start));
        return lines;
    }

    private readonly record struct LogicalLineSpan(int Start, int Length);
}
