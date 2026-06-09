using System.Text;

namespace Novolis.Avalonia.Markdown;

/// <summary>Formats right-aligned line numbers for the editor gutter.</summary>
public static class LineNumberGutterFormatter
{
    /// <summary>Counts logical lines in the given text (minimum 1).</summary>
    /// <param name="text">Editor text.</param>
    /// <returns>Line count.</returns>
    public static int CountLines(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return 1;

        var lines = 1;
        foreach (var ch in text)
        {
            if (ch == '\n')
                lines++;
        }

        return lines;
    }

    /// <summary>Determines the 1-based line index for a caret offset.</summary>
    /// <param name="text">Editor text.</param>
    /// <param name="caretIndex">Zero-based caret index.</param>
    /// <returns>1-based line number.</returns>
    public static int LineAtCaret(string? text, int caretIndex)
    {
        if (string.IsNullOrEmpty(text) || caretIndex <= 0)
            return 1;

        var line = 1;
        var limit = Math.Min(caretIndex, text.Length);
        for (var i = 0; i < limit; i++)
        {
            if (text[i] == '\n')
                line++;
        }

        return line;
    }

    /// <summary>Formats line numbers for display in the gutter.</summary>
    /// <param name="lineCount">Total logical lines.</param>
    /// <param name="activeLine">1-based active line to emphasize; 0 for none.</param>
    /// <returns>Multiline gutter text.</returns>
    public static string Format(int lineCount, int activeLine = 0)
    {
        lineCount = Math.Max(1, lineCount);
        var width = lineCount.ToString().Length;
        var builder = new StringBuilder(lineCount * (width + 2));

        for (var line = 1; line <= lineCount; line++)
        {
            if (line > 1)
                builder.Append('\n');

            var number = line.ToString().PadLeft(width);
            builder.Append(line == activeLine ? $">{number}" : $" {number}");
        }

        return builder.ToString();
    }
}
