namespace Novolis.Avalonia.Markdown.Tests;

public class MarkdownZoomTests
{
    [Test]
    public async Task ApplyWheelDelta_ZoomsInAndClamps()
    {
        var zoomed = MarkdownZoom.ApplyWheelDelta(1.0, 1);
        await Assert.That(zoomed).IsEqualTo(1.08);

        var maxed = MarkdownZoom.ApplyWheelDelta(2.5, 1);
        await Assert.That(maxed).IsEqualTo(MarkdownZoom.Maximum);
    }

    [Test]
    public async Task ScaledFontSize_AppliesZoom()
    {
        await Assert.That(MarkdownZoom.ScaledFontSize(14, 1.5)).IsEqualTo(21.0);
    }
}
