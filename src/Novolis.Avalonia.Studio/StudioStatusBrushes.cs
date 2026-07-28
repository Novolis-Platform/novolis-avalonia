using Avalonia.Media;

namespace Novolis.Avalonia.Studio;

/// <summary>Shared brushes for studio status bars (clean vs dirty).</summary>
public static class StudioStatusBrushes
{
    /// <summary>Clean / saved status bar (VS-like blue).</summary>
    public static IBrush Clean { get; } = new SolidColorBrush(Color.Parse("#007ACC"));

    /// <summary>Dirty / unsaved status bar (amber).</summary>
    public static IBrush Dirty { get; } = new SolidColorBrush(Color.Parse("#C27A00"));

    /// <summary>Returns <see cref="Dirty"/> when <paramref name="isDirty"/> is true; otherwise <see cref="Clean"/>.</summary>
    public static IBrush ForDirtyState(bool isDirty) => isDirty ? Dirty : Clean;
}
