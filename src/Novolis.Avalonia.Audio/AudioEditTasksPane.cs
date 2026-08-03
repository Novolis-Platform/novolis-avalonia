using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Novolis.Avalonia.Audio;

/// <summary>Task column for the audio editor.</summary>
public sealed class AudioEditTasksPane : AudioEditPane
{
    /// <summary>Creates standard task buttons.</summary>
    public AudioEditTasksPane()
    {
        Title = "Tasks";
        Body = new StackPanel
        {
            Children =
            {
                Task("Import WAV…", () => ImportRequested?.Invoke()),
                Task("Add tone", () => AddToneRequested?.Invoke()),
                Task("Add track", () => AddTrackRequested?.Invoke()),
                Task("Add to track", () => AddToTrackRequested?.Invoke()),
                Task("Split at playhead", () => SplitRequested?.Invoke()),
                Task("Remove clip", () => RemoveClipRequested?.Invoke()),
                Task("Export mix WAV…", () => ExportRequested?.Invoke()),
                Task("Play / Pause", () => PlayPauseRequested?.Invoke()),
                Task("Rewind", () => RewindRequested?.Invoke()),
            },
        };
    }

    /// <summary>Import WAV requested.</summary>
    public event Action? ImportRequested;

    /// <summary>Add tone requested.</summary>
    public event Action? AddToneRequested;

    /// <summary>Add track requested.</summary>
    public event Action? AddTrackRequested;

    /// <summary>Place library sound on track.</summary>
    public event Action? AddToTrackRequested;

    /// <summary>Split requested.</summary>
    public event Action? SplitRequested;

    /// <summary>Remove clip requested.</summary>
    public event Action? RemoveClipRequested;

    /// <summary>Export requested.</summary>
    public event Action? ExportRequested;

    /// <summary>Play/pause requested.</summary>
    public event Action? PlayPauseRequested;

    /// <summary>Rewind requested.</summary>
    public event Action? RewindRequested;

    static Button Task(string label, Action action)
    {
        var b = new Button
        {
            Content = label,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 0, 0, 8),
            Padding = new Thickness(10, 8),
            Background = AudioEditPalette.Accent,
            Foreground = Brushes.White,
        };
        b.Click += (_, _) => action();
        return b;
    }
}
