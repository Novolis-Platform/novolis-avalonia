using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Audio.Edit;

namespace Novolis.Avalonia.Audio;

/// <summary>Rewind / play-pause strip.</summary>
public sealed class AudioTransportBar : StackPanel
{
    AudioTransport? _transport;

    /// <summary>Creates unbound transport buttons.</summary>
    public AudioTransportBar()
    {
        Orientation = Orientation.Horizontal;
        Spacing = 8;
        Children.Add(Make("◀◀", () => _transport?.Seek(TimeSpan.Zero)));
        Children.Add(Make("▶ / ❚❚", () => _transport?.Toggle()));
    }

    /// <summary>Binds actions to transport.</summary>
    public void Bind(AudioTransport transport) =>
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));

    static Button Make(string label, Action action)
    {
        var b = new Button
        {
            Content = label,
            Padding = new Thickness(14, 6),
            Background = AudioEditPalette.Amber,
            Foreground = Brushes.Black,
        };
        b.Click += (_, _) => action();
        return b;
    }
}
