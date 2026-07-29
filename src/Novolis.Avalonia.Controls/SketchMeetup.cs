namespace Novolis.Avalonia.Controls;

/// <summary>Object snap so new geometry can meet existing shape vertices.</summary>
public static class SketchMeetup
{
    /// <summary>
    /// Returns the nearest vertex among <paramref name="elements"/> within <paramref name="radius"/>,
    /// or null if none are close enough.
    /// </summary>
    public static SketchPoint? FindNearestVertex(
        IEnumerable<StrokeShape> elements,
        SketchPoint point,
        double radius,
        string? excludeElementId = null)
    {
        ArgumentNullException.ThrowIfNull(elements);
        var r2 = radius * radius;
        SketchPoint? best = null;
        var bestD2 = double.PositiveInfinity;

        foreach (var stroke in elements)
        {
            if (excludeElementId is not null
                && string.Equals(stroke.Id, excludeElementId, StringComparison.Ordinal))
                continue;

            foreach (var p in stroke.Points)
            {
                var dx = p.X - point.X;
                var dy = p.Y - point.Y;
                var d2 = dx * dx + dy * dy;
                if (d2 <= r2 && d2 < bestD2)
                {
                    bestD2 = d2;
                    best = p;
                }
            }
        }

        return best;
    }
}
