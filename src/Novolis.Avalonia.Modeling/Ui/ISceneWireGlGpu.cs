using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using Novolis.Avalonia.Modeling.Services;
using Novolis.Avalonia.Modeling.Session;

namespace Novolis.Avalonia.Modeling.Ui;

internal interface ISceneWireGlGpu : IDisposable
{
    void Render(SceneSessionService session, SceneViewportCamera camera, int framebuffer, int w, int h, bool rebuildLines);
    void ReadRgba(Span<byte> rgba, int w, int h);
}
