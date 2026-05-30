using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Novolis.Avalonia.Studio;

/// <summary>Status line, flash line, and busy overlay widgets for studio apps.</summary>
public sealed class StudioChrome
{
    public TextBlock StatusLine { get; }
    public TextBlock FlashLine { get; }
    public Border BusyOverlay { get; }
    public TextBlock BusyText { get; }

    private StudioChrome(TextBlock statusLine, TextBlock flashLine, Border busyOverlay, TextBlock busyText)
    {
        StatusLine = statusLine;
        FlashLine = flashLine;
        BusyOverlay = busyOverlay;
        BusyText = busyText;
    }

    public static StudioChrome Create()
    {
        var status = new TextBlock { Margin = new Thickness(8, 4) };
        var flash = new TextBlock
        {
            Margin = new Thickness(8, 0, 8, 4),
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.LightGreen,
        };
        var busyText = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 16,
            FontWeight = FontWeight.Bold,
        };
        var busyOverlay = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
            IsVisible = false,
            IsHitTestVisible = true,
            Child = busyText,
        };
        return new StudioChrome(status, flash, busyOverlay, busyText);
    }

    public StudioFeedback CreateFeedback() =>
        new(StatusLine, FlashLine, BusyOverlay, BusyText);
}
