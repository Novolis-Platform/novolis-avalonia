using Avalonia.OpenGL;
using Novolis.Avalonia.Modeling.Services;
using Novolis.Avalonia.Modeling.Session;

namespace Novolis.Avalonia.Modeling.Ui;

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
