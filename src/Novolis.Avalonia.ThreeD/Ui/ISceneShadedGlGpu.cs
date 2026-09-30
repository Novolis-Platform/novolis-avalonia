using Avalonia.OpenGL;
using Novolis.Avalonia.ThreeD.Services;
using Novolis.Avalonia.ThreeD.Session;

namespace Novolis.Avalonia.ThreeD.Ui;

internal interface ISceneShadedGlGpu : IDisposable
{
    void Render(
        SceneSessionService session,
        SceneViewportCamera camera,
        SceneRenderSettings settings,
        int framebuffer,
        int w,
        int h,
        bool rebuildMesh);
    void ReadRgba(Span<byte> rgba, int w, int h);
}
