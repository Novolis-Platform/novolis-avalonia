using Avalonia.OpenGL;
using Novolis.Avalonia.ThreeD.Session;

namespace Novolis.Avalonia.ThreeD.Ui;

/// <summary>Registers Silk GPU factory — loaded only when <see cref="SceneWireGlBootstrap"/> demands it.</summary>
internal static class SceneWireGlGpuFactory
{
    static SceneWireGlGpuFactory() =>
        SceneWireGlBootstrap.CreateImpl = static gl => new SceneWireGlGpu(gl);
}
