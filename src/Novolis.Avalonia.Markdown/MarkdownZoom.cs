namespace Novolis.Avalonia.Markdown;

/// <summary>Helpers for Ctrl+scroll zoom scaling.</summary>
public static class MarkdownZoom
{
    /// <summary>Default zoom multiplier (100%).</summary>
    public const double Default = 1.0;

    /// <summary>Minimum allowed zoom multiplier.</summary>
    public const double Minimum = 0.6;

    /// <summary>Maximum allowed zoom multiplier.</summary>
    public const double Maximum = 2.5;

    /// <summary>Zoom step per mouse wheel notch when Ctrl is held.</summary>
    public const double Step = 0.08;

    /// <summary>Applies a mouse wheel delta to the current zoom scale.</summary>
    /// <param name="current">Current zoom scale.</param>
    /// <param name="wheelDeltaY">Mouse wheel Y delta (positive = zoom in).</param>
    /// <returns>Clamped zoom scale.</returns>
    public static double ApplyWheelDelta(double current, double wheelDeltaY)
    {
        var next = current + (wheelDeltaY > 0 ? Step : -Step);
        return Math.Clamp(next, Minimum, Maximum);
    }

    /// <summary>Computes a font size from a base size and zoom scale.</summary>
    /// <param name="baseFontSize">Unscaled font size in device-independent pixels.</param>
    /// <param name="zoomScale">Current zoom scale.</param>
    /// <returns>Scaled font size.</returns>
    public static double ScaledFontSize(double baseFontSize, double zoomScale) =>
        Math.Round(baseFontSize * zoomScale, 1);
}
