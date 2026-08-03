using Avalonia.Media;

namespace Novolis.Avalonia.Video;

/// <summary>Shared brushes for Movie Maker–style edit chrome.</summary>
public static class MovieEditPalette
{
    /// <summary>Window / deep background.</summary>
    public static readonly IBrush Pane = new SolidColorBrush(Color.FromRgb(22, 32, 48));

    /// <summary>Panel fill.</summary>
    public static readonly IBrush PaneAlt = new SolidColorBrush(Color.FromRgb(30, 44, 62));

    /// <summary>Panel border.</summary>
    public static readonly IBrush Border = new SolidColorBrush(Color.FromRgb(55, 75, 95));

    /// <summary>Primary action / section title accent.</summary>
    public static readonly IBrush Accent = new SolidColorBrush(Color.FromRgb(40, 140, 150));

    /// <summary>Transport CTA accent.</summary>
    public static readonly IBrush Amber = new SolidColorBrush(Color.FromRgb(220, 150, 60));
}
