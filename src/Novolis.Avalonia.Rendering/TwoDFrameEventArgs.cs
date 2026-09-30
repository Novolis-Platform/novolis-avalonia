using System.Diagnostics;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Rendering;
using Novolis.Rendering.Backends.TwoD.Silk;
using Novolis.Rendering.TwoD;
using Silk.NET.OpenGL;
using PresentationMouseButton = Novolis.Rendering.Presentation.MouseButton;
using AvaloniaMouseButton = global::Avalonia.Input.MouseButton;

namespace Novolis.Avalonia.Rendering;

/// <summary>Per-frame update args for <see cref="TwoDSceneControl"/>.</summary>
public sealed class TwoDFrameEventArgs(float deltaSeconds) : EventArgs
{
    /// <summary>Elapsed time since the previous frame in seconds.</summary>
    public float DeltaSeconds { get; } = deltaSeconds;
}
