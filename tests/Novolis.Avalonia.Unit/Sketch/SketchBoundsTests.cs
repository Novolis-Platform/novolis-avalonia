using Novolis.Avalonia.Controls.Sketch;

namespace Novolis.Avalonia.Unit.Controls;

public sealed class SketchBoundsTests
{
    [Test]
    public async Task FromPoints_Computes_Aabb()
    {
        var bounds = SketchBounds.FromPoints(
        [
            new SketchPoint(10, 20),
            new SketchPoint(40, 5),
            new SketchPoint(15, 50)
        ]);
        await Assert.That(bounds).IsEqualTo(new SketchRect(10, 5, 30, 45));
    }

    [Test]
    public async Task ApplyBoundsTransform_Scales_Points()
    {
        var points = new List<SketchPoint>
        {
            new(0, 0),
            new(10, 0),
            new(10, 10)
        };
        var oldBounds = new SketchRect(0, 0, 10, 10);
        var newBounds = new SketchRect(0, 0, 20, 40);
        SketchBounds.ApplyBoundsTransform(points, oldBounds, newBounds);
        await Assert.That(points).IsEquivalentTo(new[]
        {
            new SketchPoint(0, 0),
            new SketchPoint(20, 0),
            new SketchPoint(20, 40)
        });
    }

    [Test]
    public async Task DistanceToPolyline_Hits_Segment()
    {
        var points = new[] { new SketchPoint(0, 0), new SketchPoint(10, 0) };
        var d = SketchBounds.DistanceToPolyline(points, new SketchPoint(5, 2));
        await Assert.That(d).IsEqualTo(2.0);
    }

    [Test]
    public async Task RotatePoint_Quarter_Turn()
    {
        var rotated = SketchBounds.RotatePoint(new SketchPoint(1, 0), new SketchPoint(0, 0), 90);
        await Assert.That(rotated.X).IsEqualTo(0.0).Within(1e-9);
        await Assert.That(rotated.Y).IsEqualTo(1.0).Within(1e-9);
    }

    [Test]
    public async Task RotatedAabb_Expands_For_Diamond()
    {
        var pts = new[]
        {
            new SketchPoint(0, 0),
            new SketchPoint(10, 0),
            new SketchPoint(10, 10),
            new SketchPoint(0, 10)
        };
        var aabb = SketchBounds.RotatedAabb(pts, 45);
        await Assert.That(aabb.Width).IsGreaterThan(10);
        await Assert.That(aabb.Height).IsGreaterThan(10);
    }

    [Test]
    public async Task DistanceToRotatedPolyline_Matches_Inverse()
    {
        var pts = new[] { new SketchPoint(0, 0), new SketchPoint(10, 0) };
        var center = SketchBounds.FromPoints(pts).Center;
        var world = SketchBounds.RotatePoint(new SketchPoint(5, 2), center, 30);
        var d = SketchBounds.DistanceToRotatedPolyline(pts, 30, world);
        await Assert.That(d).IsEqualTo(2.0).Within(1e-9);
    }
}
