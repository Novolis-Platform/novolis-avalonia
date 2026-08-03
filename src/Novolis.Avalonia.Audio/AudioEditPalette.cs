using Avalonia.Media;

namespace Novolis.Avalonia.Audio;

/// <summary>Shared brushes for Magix / Audacity–style edit chrome.</summary>
public static class AudioEditPalette
{
    /// <summary>Deep background.</summary>
    public static readonly IBrush Pane = new SolidColorBrush(Color.FromRgb(24, 28, 34));

    /// <summary>Panel fill.</summary>
    public static readonly IBrush PaneAlt = new SolidColorBrush(Color.FromRgb(36, 42, 52));

    /// <summary>Panel border.</summary>
    public static readonly IBrush Border = new SolidColorBrush(Color.FromRgb(60, 70, 85));

    /// <summary>Accent (teal).</summary>
    public static readonly IBrush Accent = new SolidColorBrush(Color.FromRgb(50, 160, 140));

    /// <summary>Transport / selection.</summary>
    public static readonly IBrush Amber = new SolidColorBrush(Color.FromRgb(230, 170, 70));

    /// <summary>Waveform stroke.</summary>
    public static readonly IBrush Wave = new SolidColorBrush(Color.FromRgb(120, 200, 180));
}
