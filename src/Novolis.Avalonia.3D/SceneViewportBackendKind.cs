namespace Novolis.Avalonia._3D;

/// <summary>
/// Interactive CAD / 3D wireframe presenters.
/// <see cref="OpenGl"/> is the product default for authoring; other values are for bench and fallbacks.
/// </summary>
public enum SceneViewportBackendKind
{
    /// <summary>
    /// Avalonia OpenGlControlBase + Silk — use this for CAD and 3D work.
    /// Native GL present, interactive orbit, no CPU framebuffer readback.
    /// </summary>
    OpenGl = 0,

    /// <summary>CPU Bresenham into Rgba32FrameControl (software fallback / parity checks).</summary>
    Cpu = 1,

    /// <summary>Embedded Raylib GLFW stream — legacy compare host; not the CAD default.</summary>
    Raylib = 2,

    /// <summary>
    /// Vulkan graphics wire + CPU readback into Rgba32FrameControl.
    /// API parity / ViewportBench only — not the primary interactive CAD path.
    /// </summary>
    Vulkan = 3,
}
