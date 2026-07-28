using Novolis.Avalonia.Studio;

namespace Novolis.Avalonia.Unit.Studio;

public sealed class StudioStatusBrushesTests
{
    [Test]
    public async Task ForDirtyState_Switches()
    {
        await Assert.That(StudioStatusBrushes.ForDirtyState(true)).IsEqualTo(StudioStatusBrushes.Dirty);
        await Assert.That(StudioStatusBrushes.ForDirtyState(false)).IsEqualTo(StudioStatusBrushes.Clean);
    }
}
