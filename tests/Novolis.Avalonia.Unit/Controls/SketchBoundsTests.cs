using Novolis.Avalonia.Controls;

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
}
