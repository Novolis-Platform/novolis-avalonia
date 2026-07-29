using Novolis.Avalonia.Controls;

namespace Novolis.Avalonia.Unit.Controls;

public sealed class SketchGridifyTests
{
    [Test]
    public async Task Gridify_Snaps_Points_To_Grid()
    {
        var points = new[]
        {
            new SketchPoint(3, 7),
            new SketchPoint(12, 18),
            new SketchPoint(23, 21)
        };
        var result = SketchGridify.Gridify(points, 10);
        await Assert.That(result).IsEquivalentTo(new[]
        {
            new SketchPoint(0, 10),
            new SketchPoint(10, 20),
            new SketchPoint(20, 20)
        });
    }

    [Test]
    public async Task Gridify_Dedupes_Consecutive_Duplicates()
    {
        var points = new[]
        {
            new SketchPoint(1, 1),
            new SketchPoint(2, 2),
            new SketchPoint(3, 3)
        };
        var result = SketchGridify.Gridify(points, 10);
        await Assert.That(result).IsEquivalentTo(new[] { new SketchPoint(0, 0) });
    }

    [Test]
    public async Task Gridify_Collapses_Collinear_Ortholinear_Runs()
    {
        var points = new[]
        {
            new SketchPoint(0, 0),
            new SketchPoint(10, 0),
            new SketchPoint(20, 0),
            new SketchPoint(20, 10)
        };
        var result = SketchGridify.Gridify(points, 10);
        await Assert.That(result).IsEquivalentTo(new[]
        {
            new SketchPoint(0, 0),
            new SketchPoint(20, 0),
            new SketchPoint(20, 10)
        });
    }

    [Test]
    public async Task Snap_Rounds_To_Nearest_Intersection()
    {
        await Assert.That(SketchSnap.Snap(new SketchPoint(14, 26), 10))
            .IsEqualTo(new SketchPoint(10, 30));
    }
}
