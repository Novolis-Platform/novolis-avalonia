namespace Novolis.Avalonia.Raylib;

/// <summary>
/// Process-wide GLFW host policy for Avalonia viewports.
/// Raylib allows one embedded window per process — hidden or detached controls must not start one.
/// </summary>
public static class RaylibHostActivation
{
    /// <summary>True when this control may start the exclusive GLFW host.</summary>
    public static bool ShouldStartHost(bool attachedToVisualTree, bool effectivelyVisible) =>
        attachedToVisualTree && effectivelyVisible;
}
