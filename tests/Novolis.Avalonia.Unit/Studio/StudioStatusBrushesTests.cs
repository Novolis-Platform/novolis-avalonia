using Avalonia.Media;
using Novolis.Avalonia.Studio;

namespace Novolis.Avalonia.Unit.Studio;

public sealed class StudioStatusBrushesTests
{
    [Test]
    public async Task ForDirtyState_Switches()
    {
        var dirty = ColorOf(StudioStatusBrushes.ForDirtyState(true));
        var clean = ColorOf(StudioStatusBrushes.ForDirtyState(false));
        await Assert.That(dirty).IsEqualTo(ColorOf(StudioStatusBrushes.Dirty));
        await Assert.That(clean).IsEqualTo(ColorOf(StudioStatusBrushes.Clean));
        await Assert.That(dirty).IsNotEqualTo(clean);
    }

    static Color ColorOf(IBrush brush) => ((ISolidColorBrush)brush).Color;
}
