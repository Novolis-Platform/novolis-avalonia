namespace Novolis.Avalonia.Raylib;

/// <summary>Render-thread work queue item.</summary>
public readonly record struct HostRenderRequest(HostRenderRequestKind Kind);
