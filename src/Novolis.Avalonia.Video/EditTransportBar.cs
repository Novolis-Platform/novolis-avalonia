using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Video.Edit;

namespace Novolis.Avalonia.Video;

/// <summary>Rewind / play-pause strip bound to an <see cref="EditTransport"/>.</summary>
public sealed class EditTransportBar : StackPanel
{
    EditTransport? _transport;

    /// <summary>Creates transport buttons (unbound until <see cref="Bind"/>).</summary>
    public EditTransportBar()
    {
        Orientation = Orientation.Horizontal;
        Spacing = 8;
        Children.Add(MakeButton("◀◀", () => _transport?.Seek(TimeSpan.Zero)));
        Children.Add(MakeButton("▶ / ❚❚", () => _transport?.Toggle()));
    }

    /// <summary>Binds button actions to <paramref name="transport"/>.</summary>
    public void Bind(EditTransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    static Button MakeButton(string label, Action action)
    {
        var b = new Button
        {
            Content = label,
            Padding = new Thickness(14, 6),
            Background = MovieEditPalette.Amber,
            Foreground = Brushes.Black,
        };
        b.Click += (_, _) => action();
        return b;
    }
}
