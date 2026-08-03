using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Audio.Edit;

namespace Novolis.Avalonia.Audio;

/// <summary>Rewind / play-pause strip that raises UI events (does not silently toggle transport alone).</summary>
public sealed class AudioTransportBar : StackPanel
{
    readonly Button _play;

    /// <summary>Creates unbound transport buttons.</summary>
    public AudioTransportBar()
    {
        Orientation = Orientation.Horizontal;
        Spacing = 8;
        Children.Add(Make("◀◀", () => RewindRequested?.Invoke(), AudioEditPalette.Amber));
        _play = Make("▶ Play", () => PlayPauseRequested?.Invoke(), AudioEditPalette.Amber);
        Children.Add(_play);
    }

    /// <summary>Play / pause clicked.</summary>
    public event Action? PlayPauseRequested;

    /// <summary>Rewind to start clicked.</summary>
    public event Action? RewindRequested;

    /// <summary>Updates the play button caption from transport state.</summary>
    public void SetPlaying(bool playing) =>
        _play.Content = playing ? "❚❚ Pause" : "▶ Play";

    /// <summary>Kept for call-site compatibility; transport is driven via events.</summary>
    public void Bind(AudioTransport transport) =>
        _ = transport ?? throw new ArgumentNullException(nameof(transport));

    static Button Make(string label, Action action, IBrush background)
    {
        var b = new Button
        {
            Content = label,
            Padding = new Thickness(14, 6),
            Background = background,
            Foreground = Brushes.Black,
        };
        b.Click += (_, _) => action();
        return b;
    }
}
