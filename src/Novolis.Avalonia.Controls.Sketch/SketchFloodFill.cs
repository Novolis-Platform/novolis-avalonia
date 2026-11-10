namespace Novolis.Avalonia.Controls.Sketch;

/// <summary>
/// Raster flood-fill for paint-bucket: strokes are barriers; returns a closed polygon in world space.
/// </summary>
public static class SketchFloodFill
{
    internal const int MaxDimension = 768;
    internal const int MaxPixels = 250_000;
    internal const double PadWorld = 8;

    /// <summary>
    /// Flood-fills from <paramref name="seed"/> treating visible stroke polylines (and closed edges)
    /// as barriers. Returns contour points (closed, first≈last) or null when unbounded / empty.
    /// </summary>
    public static IReadOnlyList<SketchPoint>? TryCreateRegion(
        IEnumerable<StrokeShape> elements,
        Func<string?, bool> isLayerVisible,
        SketchPoint seed)
    {
        ArgumentNullException.ThrowIfNull(elements);
        ArgumentNullException.ThrowIfNull(isLayerVisible);

        var strokes = SketchFloodFillRaster.CollectStrokes(elements, isLayerVisible);
        if (strokes.Count == 0)
            return null;

        var layout = SketchFloodFillRaster.CreateLayout(strokes, seed);
        var barriers = SketchFloodFillRaster.BuildBarriers(layout, strokes);
        if (!SketchFloodFillRaster.TryResolveSeedIndex(barriers, layout, seed, out var sx, out var sy))
            return null;

        var filled = SketchFloodFillRaster.FloodFill(barriers, layout, sx, sy);
        if (filled is null)
            return null;

        return SketchFloodFillRaster.ToWorldRegion(filled, layout);
    }

    internal static IReadOnlyList<SketchPoint>? BoundsOfFilled(
        bool[] filled,
        int width,
        int height,
        double minX,
        double minY,
        double scale)
    {
        var minFX = width;
        var minFY = height;
        var maxFX = 0;
        var maxFY = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (!filled[y * width + x])
                    continue;
                if (x < minFX) minFX = x;
                if (y < minFY) minFY = y;
                if (x > maxFX) maxFX = x;
                if (y > maxFY) maxFY = y;
            }
        }

        if (maxFX <= minFX || maxFY <= minFY)
            return null;

        return
        [
            new SketchPoint(minX + (minFX + 0.5) / scale, minY + (minFY + 0.5) / scale),
            new SketchPoint(minX + (maxFX + 0.5) / scale, minY + (minFY + 0.5) / scale),
            new SketchPoint(minX + (maxFX + 0.5) / scale, minY + (maxFY + 0.5) / scale),
            new SketchPoint(minX + (minFX + 0.5) / scale, minY + (maxFY + 0.5) / scale),
            new SketchPoint(minX + (minFX + 0.5) / scale, minY + (minFY + 0.5) / scale)
        ];
    }

    internal static void Dilate(bool[] map, int width, int height)
    {
        var copy = (bool[])map.Clone();
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (!copy[y * width + x])
                    continue;
                for (var dy = -1; dy <= 1; dy++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var xx = x + dx;
                        var yy = y + dy;
                        if ((uint)xx >= (uint)width || (uint)yy >= (uint)height)
                            continue;
                        map[yy * width + xx] = true;
                    }
                }
            }
        }
    }

    internal static bool TryFindEmptyNear(bool[] barriers, int width, int height, int sx, int sy, out int x, out int y)
    {
        for (var r = 1; r <= 6; r++)
        {
            for (var dy = -r; dy <= r; dy++)
            {
                for (var dx = -r; dx <= r; dx++)
                {
                    var xx = sx + dx;
                    var yy = sy + dy;
                    if ((uint)xx >= (uint)width || (uint)yy >= (uint)height)
                        continue;
                    if (!barriers[yy * width + xx])
                    {
                        x = xx;
                        y = yy;
                        return true;
                    }
                }
            }
        }

        x = sx;
        y = sy;
        return false;
    }

    internal static bool IsNearlyClosed(IReadOnlyList<SketchPoint> pts) =>
        pts.Count >= 3 && Distance(pts[0], pts[^1]) < 1e-6;

    internal static double Distance(SketchPoint a, SketchPoint b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    internal static List<(int X, int Y)> TraceOuterContour(bool[] filled, int width, int height)
    {
        var start = (-1, -1);
        for (var y = 0; y < height && start.Item1 < 0; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (filled[y * width + x])
                {
                    start = (x, y);
                    break;
                }
            }
        }

        if (start.Item1 < 0)
            return [];

        // Moore neighborhood clockwise from west.
        ReadOnlySpan<(int Dx, int Dy)> dirs =
        [
            (1, 0), (1, 1), (0, 1), (-1, 1),
            (-1, 0), (-1, -1), (0, -1), (1, -1)
        ];

        var path = new List<(int X, int Y)>();
        var (cx, cy) = start;
        var dir = 4; // came from west → look from west
        var guard = width * height * 4;
        do
        {
            path.Add((cx, cy));
            var startDir = (dir + 6) % 8; // turn left relative to incoming
            var found = false;
            for (var k = 0; k < 8; k++)
            {
                var d = (startDir + k) % 8;
                var nx = cx + dirs[d].Dx;
                var ny = cy + dirs[d].Dy;
                if ((uint)nx >= (uint)width || (uint)ny >= (uint)height)
                    continue;
                if (!filled[ny * width + nx])
                    continue;
                cx = nx;
                cy = ny;
                dir = d;
                found = true;
                break;
            }

            if (!found)
                break;
            guard--;
        } while ((cx != start.Item1 || cy != start.Item2) && guard > 0);

        return path;
    }

    internal static List<SketchPoint> Simplify(IReadOnlyList<SketchPoint> points, double epsilon)
    {
        if (points.Count < 3)
            return [.. points];

        var keep = new bool[points.Count];
        keep[0] = true;
        keep[^1] = true;
        SimplifySection(points, 0, points.Count - 1, epsilon, keep);

        var result = new List<SketchPoint>();
        for (var i = 0; i < points.Count; i++)
        {
            if (keep[i])
                result.Add(points[i]);
        }

        return result;
    }

    static void SimplifySection(
        IReadOnlyList<SketchPoint> points,
        int first,
        int last,
        double epsilon,
        bool[] keep)
    {
        if (last <= first + 1)
            return;

        var maxDist = 0.0;
        var index = -1;
        var a = points[first];
        var b = points[last];
        for (var i = first + 1; i < last; i++)
        {
            var d = PerpDistance(points[i], a, b);
            if (d > maxDist)
            {
                maxDist = d;
                index = i;
            }
        }

        if (index < 0 || maxDist <= epsilon)
            return;

        keep[index] = true;
        SimplifySection(points, first, index, epsilon, keep);
        SimplifySection(points, index, last, epsilon, keep);
    }

    static double PerpDistance(SketchPoint p, SketchPoint a, SketchPoint b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var len2 = dx * dx + dy * dy;
        if (len2 < 1e-12)
            return Distance(p, a);
        var t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / len2;
        var proj = new SketchPoint(a.X + t * dx, a.Y + t * dy);
        return Distance(p, proj);
    }
}
