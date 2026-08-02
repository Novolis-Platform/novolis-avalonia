using System.Numerics;
using Avalonia;
using Novolis.Avalonia.Cad.Services;

namespace Novolis.Avalonia.Unit.Cad;

public sealed class CadModelPickTests
{
    [Test]
    public async Task TryHitElevationPlane_HitsCenterPixel()
    {
        var eye = new Vector3(0f, 10f, 10f);
        var target = Vector3.Zero;
        var size = new Size(800, 600);
        var ok = CadModelPick.TryHitElevationPlane(eye, target, 60f, size, new Point(400, 300), 0f, out var hit);
        await Assert.That(ok).IsTrue();
        await Assert.That(MathF.Abs(hit.Y)).IsLessThan(0.01f);
    }

    [Test]
    public async Task TryHitElevationPlane_RejectsTinyViewport()
    {
        var ok = CadModelPick.TryHitElevationPlane(
            Vector3.UnitY * 5f,
            Vector3.Zero,
            60f,
            new Size(0, 0),
            new Point(0, 0),
            0f,
            out _);
        await Assert.That(ok).IsFalse();
    }

    [Test]
    public async Task TryHitVerticalPlaneFacingCamera_HitsNearAnchor()
    {
        var eye = new Vector3(5f, 2f, 5f);
        var target = Vector3.Zero;
        var anchor = new Vector3(1f, 0f, 0f);
        var ok = CadModelPick.TryHitVerticalPlaneFacingCamera(
            eye, target, 60f, new Size(640, 480), new Point(320, 240), anchor, out var hit);
        await Assert.That(ok).IsTrue();
        await Assert.That(hit.X).IsGreaterThan(0.3f);
    }
}
