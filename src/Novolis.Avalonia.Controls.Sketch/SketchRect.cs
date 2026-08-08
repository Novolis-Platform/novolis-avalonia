namespace Novolis.Avalonia.Controls.Sketch;

/// <summary>Axis-aligned bounding rectangle in world space.</summary>
public readonly record struct SketchRect(double X, double Y, double Width, double Height)
{
    /// <summary>Right edge (X + Width).</summary>
    public double Right => X + Width;

    /// <summary>Bottom edge (Y + Height).</summary>
    public double Bottom => Y + Height;

    /// <summary>Center of the rectangle.</summary>
    public SketchPoint Center => new(X + Width * 0.5, Y + Height * 0.5);

    /// <summary>Whether the rectangle has non-zero area.</summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;

    /// <summary>Whether <paramref name="point"/> lies inside (inclusive).</summary>
    public bool Contains(SketchPoint point) =>
        point.X >= X && point.X <= Right && point.Y >= Y && point.Y <= Bottom;

    /// <summary>Inflates by <paramref name="padding"/> on all sides.</summary>
    public SketchRect Inflate(double padding) =>
        new(X - padding, Y - padding, Width + padding * 2, Height + padding * 2);
}
