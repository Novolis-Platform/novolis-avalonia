namespace Novolis.Avalonia.Controls.Sketch;

/// <summary>Sketch canvas grid configuration.</summary>
public sealed class GridSettings
{
    double _size = 20;

    /// <summary>Grid cell size in world units (minimum 1).</summary>
    public double Size
    {
        get => _size;
        set => _size = Math.Max(1, value);
    }

    /// <summary>Whether the grid is drawn.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>Whether pointer edits snap to grid intersections.</summary>
    public bool SnapEnabled { get; set; }
}
