using TUnit.Core;

namespace Novolis.Avalonia.Controls.Tests;

public class HexDumpFormatterTests
{
    [Test]
    public async Task Format_empty_returns_empty()
    {
        await Assert.That(HexDumpFormatter.Format([])).IsEmpty();
    }

    [Test]
    public async Task Format_single_line_includes_offset_and_ascii()
    {
        var bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
        var dump = HexDumpFormatter.Format(bytes);
        await Assert.That(dump).Contains("00000000");
        await Assert.That(dump).Contains("48 65 6C 6C 6F");
        await Assert.That(dump).Contains("|Hello|");
    }

    [Test]
    public async Task Format_multiline_uses_next_offset()
    {
        var bytes = new byte[20];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = (byte)i;

        var dump = HexDumpFormatter.Format(bytes, bytesPerLine: 8);
        await Assert.That(dump).Contains("00000000");
        await Assert.That(dump).Contains("00000008");
        await Assert.That(dump).Contains("|........|");
    }

    [Test]
    public async Task Format_non_printable_bytes_render_as_dots()
    {
        var bytes = new byte[] { 0x01, 0x02, 0x7F, 0x20 };
        var dump = HexDumpFormatter.Format(bytes);

        await Assert.That(dump).Contains("|... |");
    }
}
