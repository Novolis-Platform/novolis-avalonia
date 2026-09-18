using Avalonia.OpenGL;
using Novolis.Avalonia.ThreeD.Session;

namespace Novolis.Avalonia.ThreeD.Ui;

/// <summary>Late-bound GPU factory so Silk.NET is not loaded until OpenGL init.</summary>
internal static class SceneWireGlBootstrap
{
    internal static Func<GlInterface, ISceneWireGlGpu>? CreateImpl;

    public static ISceneWireGlGpu Create(GlInterface gl)
    {
        if (CreateImpl is null)
        {
            var type = Type.GetType("Novolis.Avalonia.ThreeD.Ui.SceneWireGlGpuFactory, Novolis.Avalonia.ThreeD", throwOnError: true)!;
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            if (CreateImpl is null)
                throw new InvalidOperationException("SceneWireGlGpuFactory did not register.");
        }

        return CreateImpl(gl);
    }
}
