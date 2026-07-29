namespace Novolis.Avalonia.Controls;

/// <summary>Primitive polyline builders for box, ellipse, and spline tools.</summary>
public static class SketchPrimitives
{
    /// <summary>Axis-aligned rectangle as a closed polyline (5 points).</summary>
    public static List<SketchPoint> Rect(SketchPoint a, SketchPoint b)
    {
        var left = Math.Min(a.X, b.X);
        var top = Math.Min(a.Y, b.Y);
        var right = Math.Max(a.X, b.X);
        var bottom = Math.Max(a.Y, b.Y);
        return
        [
            new SketchPoint(left, top),
            new SketchPoint(right, top),
            new SketchPoint(right, bottom),
            new SketchPoint(left, bottom),
            new SketchPoint(left, top)
        ];
    }

    /// <summary>
    /// Ellipse inscribed in the AABB of <paramref name="a"/> and <paramref name="b"/>.
    /// When <paramref name="forceCircle"/> is true, uses the larger axis as diameter.
    /// </summary>
    public static List<SketchPoint> Ellipse(SketchPoint a, SketchPoint b, bool forceCircle = false, int segments = 48)
    {
        segments = Math.Clamp(segments, 8, 256);
        var cx = (a.X + b.X) * 0.5;
        var cy = (a.Y + b.Y) * 0.5;
        var rx = Math.Abs(b.X - a.X) * 0.5;
        var ry = Math.Abs(b.Y - a.Y) * 0.5;
        if (forceCircle)
        {
            var r = Math.Max(rx, ry);
            rx = r;
            ry = r;
        }

        var points = new List<SketchPoint>(segments + 1);
        for (var i = 0; i <= segments; i++)
        {
            var t = i / (double)segments * Math.PI * 2;
            points.Add(new SketchPoint(cx + rx * Math.Cos(t), cy + ry * Math.Sin(t)));
        }

        return points;
    }

    /// <summary>
    /// Tessellates a Catmull-Rom spline through <paramref name="controls"/>
    /// (endpoints are duplicated so the curve reaches the first/last control).
    /// </summary>
    public static List<SketchPoint> CatmullRom(IReadOnlyList<SketchPoint> controls, int samplesPerSegment = 12)
    {
        ArgumentNullException.ThrowIfNull(controls);
        if (controls.Count == 0)
            return [];
        if (controls.Count == 1)
            return [controls[0]];
        if (controls.Count == 2)
            return [controls[0], controls[1]];

        samplesPerSegment = Math.Clamp(samplesPerSegment, 2, 64);
        var pts = new List<SketchPoint> { controls[0] };
        for (var i = 0; i < controls.Count - 1; i++)
        {
            var p0 = controls[Math.Max(0, i - 1)];
            var p1 = controls[i];
            var p2 = controls[i + 1];
            var p3 = controls[Math.Min(controls.Count - 1, i + 2)];
            for (var s = 1; s <= samplesPerSegment; s++)
            {
                var t = s / (double)samplesPerSegment;
                pts.Add(EvalCatmull(p0, p1, p2, p3, t));
            }
        }

        return pts;
    }

    static SketchPoint EvalCatmull(SketchPoint p0, SketchPoint p1, SketchPoint p2, SketchPoint p3, double t)
    {
        var t2 = t * t;
        var t3 = t2 * t;
        var x = 0.5 * ((2 * p1.X) + (-p0.X + p2.X) * t
                       + (2 * p0.X - 5 * p1.X + 4 * p2.X - p3.X) * t2
                       + (-p0.X + 3 * p1.X - 3 * p2.X + p3.X) * t3);
        var y = 0.5 * ((2 * p1.Y) + (-p0.Y + p2.Y) * t
                       + (2 * p0.Y - 5 * p1.Y + 4 * p2.Y - p3.Y) * t2
                       + (-p0.Y + 3 * p1.Y - 3 * p2.Y + p3.Y) * t3);
        return new SketchPoint(x, y);
    }
}
