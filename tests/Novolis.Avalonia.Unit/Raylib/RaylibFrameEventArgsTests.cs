using Novolis.Avalonia.Raylib;

namespace Novolis.Avalonia.Unit.Raylib;

public sealed class RaylibFrameEventArgsTests
{
    [Test]
    public async Task Exposes_delta_and_dimensions()
    {
        var args = new RaylibFrameEventArgs(0.016f, 800, 600);
        await Assert.That(args.DeltaSeconds).IsEqualTo(0.016f);
        await Assert.That(args.ScreenWidth).IsEqualTo(800);
        await Assert.That(args.ScreenHeight).IsEqualTo(600);
    }
}
