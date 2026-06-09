using Avalonia;
using Avalonia.Input;

namespace Novolis.Avalonia.Markdown;

/// <summary>Wires Ctrl+mouse wheel zoom on an <see cref="InputElement"/>.</summary>
public static class CtrlScrollZoom
{
    /// <summary>Registers Ctrl+scroll zoom handling on the target element.</summary>
    /// <param name="element">Element that receives wheel events.</param>
    /// <param name="getScale">Returns the current zoom scale.</param>
    /// <param name="setScale">Applies a new zoom scale.</param>
    public static void Attach(
        InputElement element,
        Func<double> getScale,
        Action<double> setScale)
    {
        element.PointerWheelChanged += (_, e) =>
        {
            if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
                return;

            var next = MarkdownZoom.ApplyWheelDelta(getScale(), e.Delta.Y);
            if (Math.Abs(next - getScale()) < 0.0001)
                return;

            setScale(next);
            e.Handled = true;
        };
    }
}
