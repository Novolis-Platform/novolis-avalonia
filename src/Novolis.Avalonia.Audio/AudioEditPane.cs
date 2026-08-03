using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Novolis.Avalonia.Audio;

/// <summary>Titled panel chrome for audio editor surfaces.</summary>
public class AudioEditPane : Border
{
    readonly TextBlock _title = new()
    {
        FontFamily = new FontFamily("Segoe UI Semibold"),
        Foreground = AudioEditPalette.Accent,
        Margin = new Thickness(0, 0, 0, 8),
        [DockPanel.DockProperty] = Dock.Top,
    };

    readonly ContentControl _bodyHost = new();

    /// <summary>Creates an empty titled pane.</summary>
    public AudioEditPane()
    {
        Background = AudioEditPalette.PaneAlt;
        BorderBrush = AudioEditPalette.Border;
        BorderThickness = new Thickness(1);
        Padding = new Thickness(10);
        Margin = new Thickness(4);
        Child = new DockPanel
        {
            LastChildFill = true,
            Children = { _title, _bodyHost },
        };
    }

    /// <summary>Section title.</summary>
    public string Title
    {
        get => _title.Text ?? string.Empty;
        set => _title.Text = value;
    }

    /// <summary>Pane body.</summary>
    public Control? Body
    {
        get => _bodyHost.Content as Control;
        set => _bodyHost.Content = value;
    }
}
