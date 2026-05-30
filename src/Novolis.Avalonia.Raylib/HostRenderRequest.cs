namespace Novolis.Avalonia.Raylib;

/// <summary>Work item for the on-demand Raylib host render thread.</summary>
public enum HostRenderRequestKind
{
    /// <summary>Redraw the current scene.</summary>
    Redraw,

    /// <summary>Resize the hidden framebuffer (coalesced with redraw).</summary>
    Resize,

    /// <summary>Stop the render thread.</summary>
    Shutdown,
}

/// <summary>Render-thread work queue item.</summary>
public readonly record struct HostRenderRequest(HostRenderRequestKind Kind);
