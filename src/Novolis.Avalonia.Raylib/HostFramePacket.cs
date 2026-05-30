using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Raylib;

/// <summary>Latest RGBA frame produced by the Raylib host thread.</summary>
public sealed class HostFramePacket
{
    internal HostFramePacket(Rgba32[] pixels, int width, int height, DateTimeOffset capturedAt)
    {
        Pixels = pixels;
        Width = width;
        Height = height;
        CapturedAt = capturedAt;
    }

    /// <summary>Top-down RGBA pixels (length <c>width * height</c>).</summary>
    public Rgba32[] Pixels { get; }

    /// <summary>Framebuffer width.</summary>
    public int Width { get; }

    /// <summary>Framebuffer height.</summary>
    public int Height { get; }

    /// <summary>UTC time when the frame was captured on the render thread.</summary>
    public DateTimeOffset CapturedAt { get; }
}
