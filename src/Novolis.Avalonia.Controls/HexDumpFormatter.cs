using System.Text;

namespace Novolis.Avalonia.Controls;

/// <summary>Formats bytes as a classic hex dump (offset, hex, ASCII).</summary>
public static class HexDumpFormatter
{
    public static string Format(ReadOnlySpan<byte> data, int bytesPerLine = 16)
    {
        if (data.IsEmpty)
            return string.Empty;

        var sb = new StringBuilder();
        for (var offset = 0; offset < data.Length; offset += bytesPerLine)
        {
            sb.Append($"{offset:X8}  ");
            var lineLength = Math.Min(bytesPerLine, data.Length - offset);
            for (var i = 0; i < bytesPerLine; i++)
            {
                if (i < lineLength)
                    sb.Append($"{data[offset + i]:X2} ");
                else
                    sb.Append("   ");

                if (i == 7)
                    sb.Append(' ');
            }

            sb.Append(" |");
            for (var i = 0; i < lineLength; i++)
            {
                var b = data[offset + i];
                sb.Append(b is >= 32 and < 127 ? (char)b : '.');
            }

            sb.Append('|');
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }
}
