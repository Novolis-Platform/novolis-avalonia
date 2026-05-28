namespace Novolis.Avalonia.Raylib;

/// <summary>Per-frame draw args for <see cref="RaylibHostControl"/>.</summary>
public sealed class RaylibFrameEventArgs(float deltaSeconds, int screenWidth, int screenHeight) : EventArgs
{
    /// <summary>Elapsed time since the previous frame in seconds.</summary>
    public float DeltaSeconds { get; } = deltaSeconds;

    /// <summary>Current Raylib drawable width in pixels.</summary>
    public int ScreenWidth { get; } = screenWidth;

    /// <summary>Current Raylib drawable height in pixels.</summary>
    public int ScreenHeight { get; } = screenHeight;
}
