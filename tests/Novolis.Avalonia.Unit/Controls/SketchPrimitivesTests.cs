using Novolis.Avalonia.Controls;

namespace Novolis.Avalonia.Unit.Controls;

public sealed class SketchPrimitivesTests
{
    [Test]
    public async Task Rect_Is_Closed()
    {
        var pts = SketchPrimitives.Rect(new SketchPoint(0, 0), new SketchPoint(10, 20));
        await Assert.That(pts.Count).IsEqualTo(5);
        await Assert.That(pts[0]).IsEqualTo(pts[^1]);
        await Assert.That(pts[2]).IsEqualTo(new SketchPoint(10, 20));
    }

    [Test]
    public async Task Ellipse_Closes()
    {
        var pts = SketchPrimitives.Ellipse(new SketchPoint(0, 0), new SketchPoint(20, 10), forceCircle: true, segments: 16);
        await Assert.That(pts.Count).IsGreaterThan(8);
        await Assert.That(pts[0].X).IsEqualTo(pts[^1].X).Within(1e-9);
        await Assert.That(pts[0].Y).IsEqualTo(pts[^1].Y).Within(1e-9);
    }

    [Test]
    public async Task CatmullRom_Produces_More_Points_Than_Controls()
    {
        var controls = new[]
        {
            new SketchPoint(0, 0),
            new SketchPoint(10, 20),
            new SketchPoint(30, 10),
            new SketchPoint(40, 0)
        };
        var curve = SketchPrimitives.CatmullRom(controls, samplesPerSegment: 8);
        await Assert.That(curve.Count).IsGreaterThan(controls.Length);
    }

    [Test]
    public async Task SmoothPolyline_Keeps_Endpoints_And_Adds_Points()
    {
        var pts = new[]
        {
            new SketchPoint(0, 0),
            new SketchPoint(10, 20),
            new SketchPoint(30, 10),
            new SketchPoint(40, 0)
        };
        var smooth = SketchPrimitives.SmoothPolyline(pts, iterations: 1);
        await Assert.That(smooth.Count).IsGreaterThan(pts.Length);
        await Assert.That(smooth[0]).IsEqualTo(pts[0]);
        await Assert.That(smooth[^1]).IsEqualTo(pts[^1]);
    }

    [Test]
    public async Task SvgDashArray_Null_For_Solid()
    {
        await Assert.That(SketchStrokeStyles.SvgDashArray(SketchStrokeStyle.Solid, 2)).IsNull();
        await Assert.That(SketchStrokeStyles.SvgDashArray(SketchStrokeStyle.Dotted, 2)).IsNotNull();
    }

    [Test]
    public async Task Meetup_Finds_Nearby_Vertex()
    {
        var stroke = new StrokeShape
        {
            Id = "a",
            Points = [new SketchPoint(0, 0), new SketchPoint(100, 0)]
        };
        var hit = SketchMeetup.FindNearestVertex([stroke], new SketchPoint(2, 1), radius: 5);
        await Assert.That(hit).IsEqualTo(new SketchPoint(0, 0));
        var miss = SketchMeetup.FindNearestVertex([stroke], new SketchPoint(50, 50), radius: 5);
        await Assert.That(miss).IsNull();
    }
}
