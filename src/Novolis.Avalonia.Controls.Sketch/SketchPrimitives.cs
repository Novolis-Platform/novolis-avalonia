namespace Novolis.Avalonia.Controls.Sketch;

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

    /// <summary>
    /// Softens freehand polylines with Chaikin corner-cutting (keeps endpoints).
    /// </summary>
    public static List<SketchPoint> SmoothPolyline(IReadOnlyList<SketchPoint> points, int iterations = 1)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 3)
            return [.. points];

        iterations = Math.Clamp(iterations, 0, 4);
        if (iterations == 0)
            return [.. points];

        var current = points.ToList();
        for (var iter = 0; iter < iterations; iter++)
        {
            var next = new List<SketchPoint>(current.Count * 2) { current[0] };
            for (var i = 0; i < current.Count - 1; i++)
            {
                var a = current[i];
                var b = current[i + 1];
                next.Add(new SketchPoint(0.75 * a.X + 0.25 * b.X, 0.75 * a.Y + 0.25 * b.Y));
                next.Add(new SketchPoint(0.25 * a.X + 0.75 * b.X, 0.25 * a.Y + 0.75 * b.Y));
            }

            next.Add(current[^1]);
            current = next;
        }

        return current;
    }

    /// <summary>
    /// Speech bubble: rounded rectangle body with a triangular tail near the bottom-left.
    /// Closed polyline starting/ending at the first body point.
    /// </summary>
    public static List<SketchPoint> SpeechBubble(SketchPoint a, SketchPoint b, int cornerSegments = 4)
    {
        cornerSegments = Math.Clamp(cornerSegments, 2, 12);
        var left = Math.Min(a.X, b.X);
        var top = Math.Min(a.Y, b.Y);
        var right = Math.Max(a.X, b.X);
        var bottom = Math.Max(a.Y, b.Y);
        var w = Math.Max(1e-6, right - left);
        var h = Math.Max(1e-6, bottom - top);
        var r = Math.Min(w, h) * 0.18;
        var bodyBottom = bottom - Math.Min(h * 0.22, Math.Max(8, h * 0.15));
        if (bodyBottom <= top + r * 2)
            bodyBottom = top + (bottom - top) * 0.75;

        var pts = new List<SketchPoint>(cornerSegments * 4 + 8);
        void Arc(double cx, double cy, double startDeg, double endDeg)
        {
            for (var i = 0; i <= cornerSegments; i++)
            {
                var t = startDeg + (endDeg - startDeg) * (i / (double)cornerSegments);
                var rad = t * Math.PI / 180.0;
                pts.Add(new SketchPoint(cx + r * Math.Cos(rad), cy + r * Math.Sin(rad)));
            }
        }

        // Top-left → top-right → bottom-right → tail → bottom-left → close
        Arc(left + r, top + r, 180, 270);
        Arc(right - r, top + r, 270, 360);
        Arc(right - r, bodyBottom - r, 0, 90);

        var tailBaseL = left + w * 0.18;
        var tailBaseR = left + w * 0.32;
        var tipX = left + w * 0.12;
        var tipY = bottom;
        pts.Add(new SketchPoint(tailBaseR, bodyBottom));
        pts.Add(new SketchPoint(tipX, tipY));
        pts.Add(new SketchPoint(tailBaseL, bodyBottom));

        Arc(left + r, bodyBottom - r, 90, 180);

        if (pts.Count > 0)
            pts.Add(pts[0]);
        return pts;
    }
}
