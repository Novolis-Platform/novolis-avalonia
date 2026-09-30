using Avalonia.Media;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;

namespace Novolis.Avalonia.Studio;

/// <summary>Shared brushes for studio status bars (clean vs dirty).</summary>
public static class StudioStatusBrushes
{
    /// <summary>Clean / saved status bar (profile navigation fill).</summary>
    public static IBrush Clean => Profile.AccentFillBrush;

    /// <summary>Dirty / unsaved status bar (profile warning).</summary>
    public static IBrush Dirty => Profile.WarningBrush;

    /// <summary>Returns <see cref="Dirty"/> when <paramref name="isDirty"/> is true; otherwise <see cref="Clean"/>.</summary>
    public static IBrush ForDirtyState(bool isDirty) => isDirty ? Dirty : Clean;
}
