using Avalonia.OpenGL;
using Novolis.Avalonia._3D.Services;
using Novolis.Avalonia._3D.Session;

namespace Novolis.Avalonia._3D.Ui;

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

/// <summary>Late-bound Silk GPU factory for shaded preview (same load-order rules as wire).</summary>
internal static class SceneShadedGlBootstrap
{
    internal static Func<GlInterface, ISceneShadedGlGpu>? CreateImpl;

    public static ISceneShadedGlGpu Create(GlInterface gl)
    {
        if (CreateImpl is null)
        {
            var type = Type.GetType("Novolis.Avalonia._3D.Ui.SceneShadedGlGpuFactory, Novolis.Avalonia.3D", throwOnError: true)!;
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            if (CreateImpl is null)
                throw new InvalidOperationException("SceneShadedGlGpuFactory did not register.");
        }

        return CreateImpl(gl);
    }
}
