namespace Novolis.Avalonia.Controls;

/// <summary>Active tool for <see cref="SketchControl"/>.</summary>
public enum SketchTool
{
    /// <summary>Freehand pen — strokes become shapes on pointer up.</summary>
    Pen = 0,

    /// <summary>Select, move, resize, and rotate committed shapes (Shift multi-select, drag marquee).</summary>
    Select = 1,

    /// <summary>Continuous polyline — click vertices; Done / Enter to finish; Close / click start / Ctrl+Enter to close; Esc cancels.</summary>
    Line = 2,

    /// <summary>Spline through clicked control points (Catmull-Rom tessellation on commit).</summary>
    Spline = 3,

    /// <summary>Axis-aligned rectangle via drag.</summary>
    Rect = 4,

    /// <summary>Ellipse / circle via drag (Shift constrains to circle).</summary>
    Ellipse = 5,

    /// <summary>Erase strokes by click or drag over them.</summary>
    Eraser = 6,

    /// <summary>Speech bubble (rounded body + tail) via drag.</summary>
    SpeechBubble = 7,

    /// <summary>Click to place a text label.</summary>
    Text = 8,

    /// <summary>Drag a bordered text box.</summary>
    TextBox = 9
}
