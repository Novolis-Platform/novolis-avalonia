namespace Novolis.Avalonia.Controls.Sketch;

/// <summary>Element kind stored on <see cref="StrokeShape"/>.</summary>
public enum SketchElementKind
{
    /// <summary>Polyline / freehand / primitive outline.</summary>
    Stroke = 0,

    /// <summary>Point-anchored text label (points[0] = anchor).</summary>
    Text = 1,

    /// <summary>Axis-aligned text box (points define placement rect).</summary>
    TextBox = 2,

    /// <summary>Raster image (points define placement rect; PNG base64 payload).</summary>
    Image = 3
}
