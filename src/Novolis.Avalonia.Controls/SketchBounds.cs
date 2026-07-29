namespace Novolis.Avalonia.Controls;

/// <summary>AABB helpers for stroke geometry (unit-testable).</summary>
public static class SketchBounds
{
    /// <summary>Computes the axis-aligned bounds of <paramref name="points"/>.</summary>
    public static SketchRect FromPoints(IReadOnlyList<SketchPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
            return default;

        var minX = points[0].X;
        var minY = points[0].Y;
        var maxX = minX;
        var maxY = minY;
        for (var i = 1; i < points.Count; i++)
        {
            var p = points[i];
            if (p.X < minX) minX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }

        return new SketchRect(minX, minY, Math.Max(0, maxX - minX), Math.Max(0, maxY - minY));
    }

    /// <summary>
    /// Maps every point from <paramref name="oldBounds"/> into <paramref name="newBounds"/>
    /// (uniform scale per axis). Degenerate old size keeps points at the new origin.
    /// </summary>
    public static void ApplyBoundsTransform(
        IList<SketchPoint> points,
        SketchRect oldBounds,
        SketchRect newBounds)
    {
        ArgumentNullException.ThrowIfNull(points);
        var sx = oldBounds.Width > 1e-9 ? newBounds.Width / oldBounds.Width : 0;
        var sy = oldBounds.Height > 1e-9 ? newBounds.Height / oldBounds.Height : 0;
        for (var i = 0; i < points.Count; i++)
        {
            var p = points[i];
            var nx = newBounds.X + (p.X - oldBounds.X) * sx;
            var ny = newBounds.Y + (p.Y - oldBounds.Y) * sy;
            points[i] = new SketchPoint(nx, ny);
        }
    }

    /// <summary>Translates all points by <paramref name="dx"/>, <paramref name="dy"/>.</summary>
    public static void Translate(IList<SketchPoint> points, double dx, double dy)
    {
        ArgumentNullException.ThrowIfNull(points);
        for (var i = 0; i < points.Count; i++)
        {
            var p = points[i];
            points[i] = new SketchPoint(p.X + dx, p.Y + dy);
        }
    }

    /// <summary>Minimum distance from <paramref name="point"/> to the polyline.</summary>
    public static double DistanceToPolyline(IReadOnlyList<SketchPoint> points, SketchPoint point)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
            return double.PositiveInfinity;
        if (points.Count == 1)
            return Distance(points[0], point);

        var best = double.PositiveInfinity;
        for (var i = 0; i < points.Count - 1; i++)
        {
            var d = DistanceToSegment(points[i], points[i + 1], point);
            if (d < best)
                best = d;
        }

        return best;
    }

    static double Distance(SketchPoint a, SketchPoint b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    static double DistanceToSegment(SketchPoint a, SketchPoint b, SketchPoint p)
    {
        var abx = b.X - a.X;
        var aby = b.Y - a.Y;
        var len2 = abx * abx + aby * aby;
        if (len2 < 1e-18)
            return Distance(a, p);

        var t = ((p.X - a.X) * abx + (p.Y - a.Y) * aby) / len2;
        t = Math.Clamp(t, 0, 1);
        var cx = a.X + t * abx;
        var cy = a.Y + t * aby;
        return Distance(new SketchPoint(cx, cy), p);
    }
}
