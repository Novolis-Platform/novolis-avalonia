using Avalonia.OpenGL;
using Novolis.Avalonia.Modeling.Session;

namespace Novolis.Avalonia.Modeling.Ui;

/// <summary>Late-bound GPU factory so Silk.NET is not loaded until OpenGL init.</summary>
internal static class SceneWireGlBootstrap
{
    internal static Func<GlInterface, ISceneWireGlGpu>? CreateImpl;

    public static ISceneWireGlGpu Create(GlInterface gl)
    {
        if (CreateImpl is null)
        {
            var type = Type.GetType("Novolis.Avalonia.Modeling.Ui.SceneWireGlGpuFactory, Novolis.Avalonia.Modeling", throwOnError: true)!;
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            if (CreateImpl is null)
                throw new InvalidOperationException("SceneWireGlGpuFactory did not register.");
        }

        return CreateImpl(gl);
    }
}
