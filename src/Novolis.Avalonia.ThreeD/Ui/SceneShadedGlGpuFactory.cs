using System.Numerics;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Novolis.Avalonia.ThreeD.Services;
using Novolis.Avalonia.ThreeD.Session;
using Novolis.Avalonia.Rendering;
using Novolis.ThreeD;
using Silk.NET.OpenGL;

namespace Novolis.Avalonia.ThreeD.Ui;

/// <summary>Registers Silk shaded GPU factory — loaded only when OpenGL init demands it.</summary>
internal static class SceneShadedGlGpuFactory
{
    static SceneShadedGlGpuFactory() =>
        SceneShadedGlBootstrap.CreateImpl = static gl => new SceneShadedGlGpu(gl);
}
