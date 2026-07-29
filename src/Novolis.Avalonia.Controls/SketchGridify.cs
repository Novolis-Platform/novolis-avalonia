namespace Novolis.Avalonia.Controls;

/// <summary>On-demand quantization of freehand strokes onto a grid.</summary>
public static class SketchGridify
{
    /// <summary>
    /// Snaps every point to the grid, collapses consecutive duplicates,
    /// and merges trivial collinear ortholinear runs.
    /// </summary>
    public static List<SketchPoint> Gridify(IReadOnlyList<SketchPoint> points, double gridSize)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
            return [];

        var snapped = new List<SketchPoint>(points.Count);
        foreach (var p in points)
        {
            var s = SketchSnap.Snap(p, gridSize);
            if (snapped.Count == 0 || snapped[^1] != s)
                snapped.Add(s);
        }

        if (snapped.Count <= 2)
            return snapped;

        return CollapseCollinear(snapped);
    }

    static List<SketchPoint> CollapseCollinear(List<SketchPoint> points)
    {
        var result = new List<SketchPoint> { points[0] };
        for (var i = 1; i < points.Count - 1; i++)
        {
            var prev = result[^1];
            var cur = points[i];
            var next = points[i + 1];
            var sameX = AlmostEqual(prev.X, cur.X) && AlmostEqual(cur.X, next.X);
            var sameY = AlmostEqual(prev.Y, cur.Y) && AlmostEqual(cur.Y, next.Y);
            if (sameX || sameY)
                continue;
            result.Add(cur);
        }

        result.Add(points[^1]);
        return result;
    }

    static bool AlmostEqual(double a, double b) => Math.Abs(a - b) < 1e-9;
}
