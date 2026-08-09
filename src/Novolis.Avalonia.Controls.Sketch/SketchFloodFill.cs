namespace Novolis.Avalonia.Controls.Sketch;

/// <summary>
/// Raster flood-fill for paint-bucket: strokes are barriers; returns a closed polygon in world space.
/// </summary>
public static class SketchFloodFill
{
    const int MaxDimension = 768;
    const int MaxPixels = 250_000;
    const double PadWorld = 8;

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

        var strokes = elements
            .Where(e => isLayerVisible(e.LayerId) && e.Kind == SketchElementKind.Stroke && e.Points.Count >= 2)
            .ToList();
        if (strokes.Count == 0)
            return null;

        var minX = seed.X;
        var minY = seed.Y;
        var maxX = seed.X;
        var maxY = seed.Y;
        var maxWidth = 2.0;
        foreach (var s in strokes)
        {
            maxWidth = Math.Max(maxWidth, s.StrokeWidth);
            foreach (var p in s.Points)
            {
                minX = Math.Min(minX, p.X);
                minY = Math.Min(minY, p.Y);
                maxX = Math.Max(maxX, p.X);
                maxY = Math.Max(maxY, p.Y);
            }
        }

        var pad = Math.Max(PadWorld, maxWidth * 2);
        minX -= pad;
        minY -= pad;
        maxX += pad;
        maxY += pad;

        var worldW = Math.Max(1e-6, maxX - minX);
        var worldH = Math.Max(1e-6, maxY - minY);
        var scale = Math.Min(MaxDimension / worldW, MaxDimension / worldH);
        scale = Math.Clamp(scale, 0.25, 8);
        var width = Math.Max(3, (int)Math.Ceiling(worldW * scale));
        var height = Math.Max(3, (int)Math.Ceiling(worldH * scale));
        if (width * height > MaxPixels)
        {
            var shrink = Math.Sqrt((double)MaxPixels / (width * height));
            scale *= shrink;
            width = Math.Max(3, (int)Math.Ceiling(worldW * scale));
            height = Math.Max(3, (int)Math.Ceiling(worldH * scale));
        }

        var barriers = new bool[width * height];
        void Stamp(int x, int y, int radius)
        {
            var r2 = radius * radius;
            for (var dy = -radius; dy <= radius; dy++)
            {
                var yy = y + dy;
                if ((uint)yy >= (uint)height)
                    continue;
                for (var dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy > r2)
                        continue;
                    var xx = x + dx;
                    if ((uint)xx >= (uint)width)
                        continue;
                    barriers[yy * width + xx] = true;
                }
            }
        }

        void StampSegment(SketchPoint a, SketchPoint b, int radius)
        {
            var ax = (a.X - minX) * scale;
            var ay = (a.Y - minY) * scale;
            var bx = (b.X - minX) * scale;
            var by = (b.Y - minY) * scale;
            var steps = Math.Max(1, (int)Math.Ceiling(Math.Max(Math.Abs(bx - ax), Math.Abs(by - ay))));
            for (var i = 0; i <= steps; i++)
            {
                var t = i / (double)steps;
                var x = (int)Math.Round(ax + (bx - ax) * t);
                var y = (int)Math.Round(ay + (by - ay) * t);
                Stamp(x, y, radius);
            }
        }

        foreach (var stroke in strokes)
        {
            var radius = Math.Max(1, (int)Math.Ceiling(Math.Max(0.5, stroke.StrokeWidth) * scale * 0.5));
            var pts = stroke.Points;
            for (var i = 0; i < pts.Count - 1; i++)
                StampSegment(pts[i], pts[i + 1], radius);
            if (stroke.Closed || IsNearlyClosed(pts))
                StampSegment(pts[^1], pts[0], radius);
        }

        var sx = (int)Math.Round((seed.X - minX) * scale);
        var sy = (int)Math.Round((seed.Y - minY) * scale);
        if ((uint)sx >= (uint)width || (uint)sy >= (uint)height)
            return null;
        if (barriers[sy * width + sx])
            return null;

        var filled = new bool[width * height];
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue((sx, sy));
        filled[sy * width + sx] = true;
        var count = 0;
        var hitBorder = false;

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            count++;
            if (count > MaxPixels)
                return null;
            if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
            {
                hitBorder = true;
                break;
            }

            TryEnqueue(x + 1, y);
            TryEnqueue(x - 1, y);
            TryEnqueue(x, y + 1);
            TryEnqueue(x, y - 1);
        }

        if (hitBorder || count < 8)
            return null;

        void TryEnqueue(int x, int y)
        {
            if ((uint)x >= (uint)width || (uint)y >= (uint)height)
                return;
            var i = y * width + x;
            if (filled[i] || barriers[i])
                return;
            filled[i] = true;
            queue.Enqueue((x, y));
        }

        var contour = TraceOuterContour(filled, width, height);
        if (contour.Count < 3)
            return null;

        var world = new List<SketchPoint>(contour.Count);
        foreach (var (x, y) in contour)
        {
            world.Add(new SketchPoint(minX + (x + 0.5) / scale, minY + (y + 0.5) / scale));
        }

        var simplified = Simplify(world, epsilon: Math.Max(0.35 / scale, 0.15));
        if (simplified.Count < 3)
            return null;
        if (Distance(simplified[0], simplified[^1]) > 1e-6)
            simplified.Add(simplified[0]);
        return simplified;
    }

    static bool IsNearlyClosed(IReadOnlyList<SketchPoint> pts) =>
        pts.Count >= 3 && Distance(pts[0], pts[^1]) < 1e-6;

    static double Distance(SketchPoint a, SketchPoint b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    static List<(int X, int Y)> TraceOuterContour(bool[] filled, int width, int height)
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

    static List<SketchPoint> Simplify(IReadOnlyList<SketchPoint> points, double epsilon)
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
