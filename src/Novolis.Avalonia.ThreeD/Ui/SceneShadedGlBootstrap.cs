using Avalonia.OpenGL;
using Novolis.Avalonia.ThreeD.Services;
using Novolis.Avalonia.ThreeD.Session;

namespace Novolis.Avalonia.ThreeD.Ui;

/// <summary>Late-bound Silk GPU factory for shaded preview (same load-order rules as wire).</summary>
internal static class SceneShadedGlBootstrap
{
    internal static Func<GlInterface, ISceneShadedGlGpu>? CreateImpl;

    public static ISceneShadedGlGpu Create(GlInterface gl)
    {
        if (CreateImpl is null)
        {
            var type = Type.GetType("Novolis.Avalonia.ThreeD.Ui.SceneShadedGlGpuFactory, Novolis.Avalonia.ThreeD", throwOnError: true)!;
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            if (CreateImpl is null)
                throw new InvalidOperationException("SceneShadedGlGpuFactory did not register.");
        }

        return CreateImpl(gl);
    }
}
