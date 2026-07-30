namespace Novolis.Avalonia._3D;

/// <summary>SceneLab viewport presenters for interactive CAD wireframe.</summary>
public enum SceneViewportBackendKind
{
    /// <summary>Avalonia OpenGL (Silk) — preferred CAD wireframe.</summary>
    OpenGl = 0,

    /// <summary>CPU Bresenham into <c>Rgba32FrameControl</c>.</summary>
    Cpu = 1,

    /// <summary>Embedded Raylib GLFW stream (legacy).</summary>
    Raylib = 2,

    /// <summary>Vulkan graphics wireframe with CPU readback into <c>Rgba32FrameControl</c>.</summary>
    Vulkan = 3,
}
