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
}
