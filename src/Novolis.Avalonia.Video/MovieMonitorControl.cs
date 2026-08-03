using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Video.Edit;

namespace Novolis.Avalonia.Video;

/// <summary>Preview monitor: <see cref="VideoSurface"/> plus <see cref="EditTransportBar"/>.</summary>
public sealed class MovieMonitorControl : MovieEditPane
{
    /// <summary>Creates a monitor with an unbound transport bar.</summary>
    public MovieMonitorControl()
    {
        Title = "Monitor";
        Surface = new VideoSurface { MinHeight = 240, Label = "Preview" };
        TransportBar = new EditTransportBar
        {
            [DockPanel.DockProperty] = Dock.Bottom,
            Margin = new Thickness(0, 8, 0, 0),
        };
        Body = new DockPanel
        {
            LastChildFill = true,
            Children =
            {
                TransportBar,
                new Border
                {
                    Background = Brushes.Black,
                    Child = Surface,
                },
            },
        };
    }

    /// <summary>Gets the preview surface.</summary>
    public VideoSurface Surface { get; }

    /// <summary>Gets the transport button strip.</summary>
    public EditTransportBar TransportBar { get; }

    /// <summary>Binds transport buttons to <paramref name="transport"/>.</summary>
    public void BindTransport(EditTransport transport) => TransportBar.Bind(transport);
}
