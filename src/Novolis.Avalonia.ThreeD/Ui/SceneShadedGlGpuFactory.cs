using Avalonia.OpenGL;
using Novolis.Avalonia.ThreeD.Session;

namespace Novolis.Avalonia.ThreeD.Ui;

/// <summary>Registers Silk shaded GPU factory — loaded only when OpenGL init demands it.</summary>
internal static class SceneShadedGlGpuFactory
{
    static SceneShadedGlGpuFactory() =>
        SceneShadedGlBootstrap.CreateImpl = static gl => new SceneShadedGlGpu(gl);
}
