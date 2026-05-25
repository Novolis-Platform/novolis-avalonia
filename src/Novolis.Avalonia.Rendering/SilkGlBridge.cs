using Avalonia.OpenGL;
using Silk.NET.OpenGL;

namespace Novolis.Avalonia.Rendering;

/// <summary>Creates a Silk.NET <see cref="GL"/> facade from an Avalonia <see cref="GlInterface"/>.</summary>
public static class SilkGlBridge
{
    /// <summary>Binds Silk.NET entry points to Avalonia's OpenGL loader.</summary>
    /// <param name="glInterface">Active Avalonia GL interface (only valid inside OpenGL callbacks).</param>
    public static GL CreateGl(GlInterface glInterface) =>
        GL.GetApi(name => glInterface.GetProcAddress(name));
}
