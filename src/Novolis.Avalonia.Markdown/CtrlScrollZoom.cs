using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Novolis.Avalonia.Markdown;

/// <summary>Wires Ctrl+mouse wheel zoom on an <see cref="InputElement"/>.</summary>
public static class CtrlScrollZoom
{
    /// <summary>
    /// Registers Ctrl+scroll zoom using a tunnel route so zoom wins over nested scroll viewers.
    /// </summary>
    /// <param name="element">Element on the wheel route (typically the scrolling surface).</param>
    /// <param name="getScale">Returns the current zoom scale.</param>
    /// <param name="setScale">Applies a new zoom scale.</param>
    public static void Attach(
        InputElement element,
        Func<double> getScale,
        Action<double> setScale)
    {
        element.AddHandler(
            InputElement.PointerWheelChangedEvent,
            (_, e) => TryHandle(e, getScale, setScale),
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
    }

    internal static bool TryHandle(
        PointerWheelEventArgs e,
        Func<double> getScale,
        Action<double> setScale)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return false;

        var next = MarkdownZoom.ApplyWheelDelta(getScale(), e.Delta.Y);
        if (Math.Abs(next - getScale()) < 0.0001)
            return false;

        setScale(next);
        e.Handled = true;
        return true;
    }
}
