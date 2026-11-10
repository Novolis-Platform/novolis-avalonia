namespace Novolis.Avalonia.Controls.Sketch;

internal readonly record struct FloodFillRasterLayout(
    double MinX,
    double MinY,
    double Scale,
    int Width,
    int Height);

internal static class SketchFloodFillRaster
{
    internal static List<StrokeShape> CollectStrokes(
        IEnumerable<StrokeShape> elements,
        Func<string?, bool> isLayerVisible) =>
        elements
            .Where(e => isLayerVisible(e.LayerId) && e.Kind == SketchElementKind.Stroke && e.Points.Count >= 2)
            .ToList();

    internal static FloodFillRasterLayout CreateLayout(IReadOnlyList<StrokeShape> strokes, SketchPoint seed)
    {
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

        var pad = Math.Max(SketchFloodFill.PadWorld, maxWidth * 2);
        minX -= pad;
        minY -= pad;
        maxX += pad;
        maxY += pad;

        var worldW = Math.Max(1e-6, maxX - minX);
        var worldH = Math.Max(1e-6, maxY - minY);
        var scale = Math.Min(SketchFloodFill.MaxDimension / worldW, SketchFloodFill.MaxDimension / worldH);
        scale = Math.Clamp(scale, 0.5, 16);
        var width = Math.Max(3, (int)Math.Ceiling(worldW * scale));
        var height = Math.Max(3, (int)Math.Ceiling(worldH * scale));
        if (width * height > SketchFloodFill.MaxPixels)
        {
            var shrink = Math.Sqrt((double)SketchFloodFill.MaxPixels / (width * height));
            scale *= shrink;
            width = Math.Max(3, (int)Math.Ceiling(worldW * scale));
            height = Math.Max(3, (int)Math.Ceiling(worldH * scale));
        }

        return new FloodFillRasterLayout(minX, minY, scale, width, height);
    }

    internal static bool[] BuildBarriers(FloodFillRasterLayout layout, IReadOnlyList<StrokeShape> strokes)
    {
        var width = layout.Width;
        var height = layout.Height;
        var barriers = new bool[width * height];
        foreach (var stroke in strokes)
        {
            var radius = Math.Max(1, (int)Math.Round(Math.Max(0.75, stroke.StrokeWidth) * layout.Scale * 0.5));
            var pts = stroke.Points;
            for (var i = 0; i < pts.Count - 1; i++)
                StampSegment(barriers, layout, pts[i], pts[i + 1], radius);
            if (stroke.Closed || SketchFloodFill.IsNearlyClosed(pts))
                StampSegment(barriers, layout, pts[^1], pts[0], radius);
        }

        SketchFloodFill.Dilate(barriers, width, height);
        return barriers;
    }

    internal static bool TryResolveSeedIndex(
        bool[] barriers,
        FloodFillRasterLayout layout,
        SketchPoint seed,
        out int sx,
        out int sy)
    {
        sx = (int)Math.Round((seed.X - layout.MinX) * layout.Scale);
        sy = (int)Math.Round((seed.Y - layout.MinY) * layout.Scale);
        if ((uint)sx >= (uint)layout.Width || (uint)sy >= (uint)layout.Height)
            return false;

        if (!barriers[sy * layout.Width + sx])
            return true;

        return SketchFloodFill.TryFindEmptyNear(barriers, layout.Width, layout.Height, sx, sy, out sx, out sy);
    }

    internal static bool[]? FloodFill(bool[] barriers, FloodFillRasterLayout layout, int sx, int sy)
    {
        var width = layout.Width;
        var height = layout.Height;
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
            if (count > SketchFloodFill.MaxPixels)
                return null;
            if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
            {
                hitBorder = true;
                break;
            }

            TryEnqueueFill(queue, filled, barriers, width, height, x + 1, y);
            TryEnqueueFill(queue, filled, barriers, width, height, x - 1, y);
            TryEnqueueFill(queue, filled, barriers, width, height, x, y + 1);
            TryEnqueueFill(queue, filled, barriers, width, height, x, y - 1);
        }

        if (hitBorder || count < 8)
            return null;

        return filled;
    }

    internal static IReadOnlyList<SketchPoint>? ToWorldRegion(
        bool[] filled,
        FloodFillRasterLayout layout)
    {
        var width = layout.Width;
        var height = layout.Height;
        var contour = SketchFloodFill.TraceOuterContour(filled, width, height);
        if (contour.Count < 3)
            return SketchFloodFill.BoundsOfFilled(filled, width, height, layout.MinX, layout.MinY, layout.Scale);

        var world = new List<SketchPoint>(contour.Count);
        foreach (var (x, y) in contour)
        {
            world.Add(new SketchPoint(
                layout.MinX + (x + 0.5) / layout.Scale,
                layout.MinY + (y + 0.5) / layout.Scale));
        }

        var simplified = SketchFloodFill.Simplify(world, epsilon: Math.Max(0.35 / layout.Scale, 0.15));
        if (simplified.Count >= 3)
        {
            if (SketchFloodFill.Distance(simplified[0], simplified[^1]) > 1e-6)
                simplified.Add(simplified[0]);
            return simplified;
        }

        return SketchFloodFill.BoundsOfFilled(filled, width, height, layout.MinX, layout.MinY, layout.Scale);
    }

    static void TryEnqueueFill(
        Queue<(int X, int Y)> queue,
        bool[] filled,
        bool[] barriers,
        int width,
        int height,
        int x,
        int y)
    {
        if ((uint)x >= (uint)width || (uint)y >= (uint)height)
            return;
        var i = y * width + x;
        if (filled[i] || barriers[i])
            return;
        filled[i] = true;
        queue.Enqueue((x, y));
    }

    static void Stamp(bool[] map, int width, int height, int x, int y, int radius)
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
                map[yy * width + xx] = true;
            }
        }
    }

    static void StampSegment(
        bool[] barriers,
        FloodFillRasterLayout layout,
        SketchPoint a,
        SketchPoint b,
        int radius)
    {
        var ax = (a.X - layout.MinX) * layout.Scale;
        var ay = (a.Y - layout.MinY) * layout.Scale;
        var bx = (b.X - layout.MinX) * layout.Scale;
        var by = (b.Y - layout.MinY) * layout.Scale;
        var steps = Math.Max(1, (int)Math.Ceiling(Math.Max(Math.Abs(bx - ax), Math.Abs(by - ay))));
        for (var i = 0; i <= steps; i++)
        {
            var t = i / (double)steps;
            var x = (int)Math.Round(ax + (bx - ax) * t);
            var y = (int)Math.Round(ay + (by - ay) * t);
            Stamp(barriers, layout.Width, layout.Height, x, y, radius);
        }
    }
}
