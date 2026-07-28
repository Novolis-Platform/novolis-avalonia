using Avalonia.Controls;

namespace Novolis.Avalonia.Studio;

/// <summary>Hides or shows chrome controls for focus / distraction-free mode.</summary>
public static class StudioFocusMode
{
    /// <summary>
    /// When <paramref name="focused"/> is true, hides each non-null control;
    /// when false, shows them again.
    /// </summary>
    public static void Apply(bool focused, params Control?[] chrome)
    {
        ArgumentNullException.ThrowIfNull(chrome);
        var visible = !focused;
        foreach (var control in chrome)
        {
            if (control is not null)
                control.IsVisible = visible;
        }
    }
}
