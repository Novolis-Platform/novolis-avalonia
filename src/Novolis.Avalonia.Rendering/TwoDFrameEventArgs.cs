using System.Diagnostics;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Rendering;

namespace Novolis.Avalonia.Rendering;

/// <summary>Per-frame update args for <see cref="TwoDSceneControl"/>.</summary>
public sealed class TwoDFrameEventArgs(float deltaSeconds) : EventArgs
{
    /// <summary>Elapsed time since the previous frame in seconds.</summary>
    public float DeltaSeconds { get; } = deltaSeconds;
}
