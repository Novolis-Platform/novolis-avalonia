namespace Novolis.Avalonia.Controls.Sketch;

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

    /// <summary>Rotates <paramref name="point"/> around <paramref name="center"/> by <paramref name="degrees"/>.</summary>
    public static SketchPoint RotatePoint(SketchPoint point, SketchPoint center, double degrees)
    {
        if (Math.Abs(degrees) < 1e-12)
            return point;
        var rad = degrees * Math.PI / 180.0;
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);
        var dx = point.X - center.X;
        var dy = point.Y - center.Y;
        return new SketchPoint(center.X + dx * cos - dy * sin, center.Y + dx * sin + dy * cos);
    }

    /// <summary>Local AABB center of <paramref name="points"/> (or origin if empty).</summary>
    public static SketchPoint LocalCenter(IReadOnlyList<SketchPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
            return default;
        return FromPoints(points).Center;
    }

    /// <summary>
    /// Axis-aligned bounds of local points after rotating around their local center by
    /// <paramref name="rotationDegrees"/>.
    /// </summary>
    public static SketchRect RotatedAabb(IReadOnlyList<SketchPoint> points, double rotationDegrees)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
            return default;
        if (Math.Abs(rotationDegrees) < 1e-12)
            return FromPoints(points);

        var center = FromPoints(points).Center;
        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;
        foreach (var p in points)
        {
            var r = RotatePoint(p, center, rotationDegrees);
            if (r.X < minX) minX = r.X;
            if (r.Y < minY) minY = r.Y;
            if (r.X > maxX) maxX = r.X;
            if (r.Y > maxY) maxY = r.Y;
        }

        return new SketchRect(minX, minY, Math.Max(0, maxX - minX), Math.Max(0, maxY - minY));
    }

    /// <summary>
    /// Distance from world <paramref name="worldPoint"/> to a rotated polyline
    /// (inverse-rotates the query into local space).
    /// </summary>
    public static double DistanceToRotatedPolyline(
        IReadOnlyList<SketchPoint> localPoints,
        double rotationDegrees,
        SketchPoint worldPoint)
    {
        ArgumentNullException.ThrowIfNull(localPoints);
        if (localPoints.Count == 0)
            return double.PositiveInfinity;
        if (Math.Abs(rotationDegrees) < 1e-12)
            return DistanceToPolyline(localPoints, worldPoint);

        var center = FromPoints(localPoints).Center;
        var local = RotatePoint(worldPoint, center, -rotationDegrees);
        return DistanceToPolyline(localPoints, local);
    }

    /// <summary>Whether <paramref name="worldPoint"/> lies inside the rotated local AABB.</summary>
    public static bool HitRotatedRect(
        IReadOnlyList<SketchPoint> localPoints,
        double rotationDegrees,
        SketchPoint worldPoint)
    {
        ArgumentNullException.ThrowIfNull(localPoints);
        if (localPoints.Count == 0)
            return false;
        var localBounds = FromPoints(localPoints);
        var center = localBounds.Center;
        var local = Math.Abs(rotationDegrees) < 1e-12
            ? worldPoint
            : RotatePoint(worldPoint, center, -rotationDegrees);
        return localBounds.Contains(local);
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
