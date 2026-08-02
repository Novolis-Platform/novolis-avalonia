using Novolis.Rendering.Presentation;

namespace Novolis.Avalonia.Rendering;

/// <summary>
/// Pointer sample in <see cref="TwoDSceneControl"/> framebuffer pixels (origin top-left, DPI-scaled).
/// </summary>
public sealed class TwoDPointerEventArgs : EventArgs
{
    /// <summary>Creates pointer args in framebuffer pixel space.</summary>
    public TwoDPointerEventArgs(
        float pixelX,
        float pixelY,
        int pixelWidth,
        int pixelHeight,
        MouseButton button,
        bool isPressed)
    {
        PixelX = pixelX;
        PixelY = pixelY;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        Button = button;
        IsPressed = isPressed;
    }

    /// <summary>X in framebuffer pixels (matches <see cref="TwoDSceneControl"/> draw size).</summary>
    public float PixelX { get; }

    /// <summary>Y in framebuffer pixels (origin top-left).</summary>
    public float PixelY { get; }

    /// <summary>Current framebuffer width in pixels.</summary>
    public int PixelWidth { get; }

    /// <summary>Current framebuffer height in pixels.</summary>
    public int PixelHeight { get; }

    /// <summary>Button that changed (for press/release) or primary for move.</summary>
    public MouseButton Button { get; }

    /// <summary>True for press, false for release; unused for move.</summary>
    public bool IsPressed { get; }

    /// <summary>Normalized X in [0,1] across the framebuffer.</summary>
    public float NormalizedX => PixelWidth > 0 ? PixelX / PixelWidth : 0f;

    /// <summary>Normalized Y in [0,1] across the framebuffer.</summary>
    public float NormalizedY => PixelHeight > 0 ? PixelY / PixelHeight : 0f;

    /// <summary>Whether the sample lies inside the framebuffer bounds.</summary>
    public bool IsInside =>
        PixelX >= 0f && PixelY >= 0f && PixelX < PixelWidth && PixelY < PixelHeight;
}
