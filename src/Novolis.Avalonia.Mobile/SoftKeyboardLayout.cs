using Avalonia;

namespace Novolis.Avalonia.Mobile;

/// <summary>
/// Pure layout math for soft-keyboard avoidance: squish by bottom inset, never overlay.
/// </summary>
public static class SoftKeyboardLayout
{
    /// <summary>
    /// Bottom inset (DIPs) to apply so content shrinks above the occluded keyboard rect.
    /// Empty / floating panes yield 0.
    /// </summary>
    public static double BottomInset(Rect occludedRect, double hostHeight)
    {
        if (hostHeight <= 0 || occludedRect.Height <= 0 || occludedRect.Width <= 0)
            return 0;

        // OccludedRect is in TopLevel client coords; prefer the portion that covers the bottom.
        var fromBottom = hostHeight - occludedRect.Y;
        if (fromBottom <= 0)
            return 0;

        var raw = global::System.Math.Min(occludedRect.Height, fromBottom);
        return global::System.Math.Clamp(raw, 0, hostHeight);
    }

    /// <summary>
    /// True when the caret's bottom edge sits at or below the visible content bottom
    /// (keyboard would cover it without a scroll).
    /// </summary>
    public static bool CaretNeedsScroll(double caretBottom, double visibleBottom, double margin = 8) =>
        caretBottom > visibleBottom - margin;

    /// <summary>Merges base padding with a keyboard bottom inset (squish, not overlay).</summary>
    public static Thickness WithKeyboardBottom(Thickness basePadding, double keyboardBottom) =>
        new(
            basePadding.Left,
            basePadding.Top,
            basePadding.Right,
            basePadding.Bottom + global::System.Math.Max(0, keyboardBottom));
}
