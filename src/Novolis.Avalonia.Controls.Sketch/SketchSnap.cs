namespace Novolis.Avalonia.Controls.Sketch;

/// <summary>Grid snap helpers (unit-testable).</summary>
public static class SketchSnap
{
    /// <summary>Snaps <paramref name="point"/> to the nearest grid intersection of size <paramref name="gridSize"/>.</summary>
    public static SketchPoint Snap(SketchPoint point, double gridSize)
    {
        var g = Math.Max(1e-9, gridSize);
        return new SketchPoint(Math.Round(point.X / g) * g, Math.Round(point.Y / g) * g);
    }
}
