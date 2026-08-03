using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Novolis.Avalonia.Video;

/// <summary>Titled panel chrome used by Movie Maker edit surfaces.</summary>
public class MovieEditPane : Border
{
    readonly TextBlock _title = new()
    {
        FontFamily = new FontFamily("Segoe UI Semibold"),
        Foreground = MovieEditPalette.Accent,
        Margin = new Thickness(0, 0, 0, 8),
        [DockPanel.DockProperty] = Dock.Top,
    };

    readonly ContentControl _bodyHost = new();

    /// <summary>Creates an empty titled pane.</summary>
    public MovieEditPane()
    {
        Background = MovieEditPalette.PaneAlt;
        BorderBrush = MovieEditPalette.Border;
        BorderThickness = new Thickness(1);
        Padding = new Thickness(10);
        Margin = new Thickness(4);
        Child = new DockPanel
        {
            LastChildFill = true,
            Children = { _title, _bodyHost },
        };
    }

    /// <summary>Creates a titled pane with content.</summary>
    public MovieEditPane(string title, Control body) : this()
    {
        Title = title;
        Body = body;
    }

    /// <summary>Section title drawn above the body.</summary>
    public string Title
    {
        get => _title.Text ?? string.Empty;
        set => _title.Text = value;
    }

    /// <summary>Pane body content.</summary>
    public Control? Body
    {
        get => _bodyHost.Content as Control;
        set => _bodyHost.Content = value;
    }
}
