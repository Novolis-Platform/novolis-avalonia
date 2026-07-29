namespace Novolis.Avalonia.Controls;

/// <summary>Active tool for <see cref="SketchControl"/>.</summary>
public enum SketchTool
{
    /// <summary>Freehand pen — strokes become shapes on pointer up.</summary>
    Pen = 0,

    /// <summary>Select, move, and resize committed shapes (Shift multi-select, drag marquee).</summary>
    Select = 1,

    /// <summary>Continuous polyline — click vertices; double-click / Enter to finish; Esc to cancel.</summary>
    Line = 2,

    /// <summary>Spline through clicked control points (Catmull-Rom tessellation on commit).</summary>
    Spline = 3,

    /// <summary>Axis-aligned rectangle via drag.</summary>
    Rect = 4,

    /// <summary>Ellipse / circle via drag (Shift constrains to circle).</summary>
    Ellipse = 5,

    /// <summary>Erase strokes by click or drag over them.</summary>
    Eraser = 6
}
