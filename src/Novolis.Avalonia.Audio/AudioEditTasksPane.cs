using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Novolis.Avalonia.Audio;

/// <summary>Task column for the audio editor (Audacity-lite ops).</summary>
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
                Task("Duplicate clip", () => DuplicateRequested?.Invoke()),
                Task("Remove clip", () => RemoveClipRequested?.Invoke()),
                Task("Normalize asset", () => NormalizeRequested?.Invoke()),
                Task("Reverse asset", () => ReverseRequested?.Invoke()),
                Task("Undo", () => UndoRequested?.Invoke()),
                Task("Redo", () => RedoRequested?.Invoke()),
                Task("Export mix WAV…", () => ExportRequested?.Invoke()),
                Task("Play / Pause", () => PlayPauseRequested?.Invoke()),
                Task("Rewind", () => RewindRequested?.Invoke()),
            },
        };
    }

    public event Action? ImportRequested;
    public event Action? AddToneRequested;
    public event Action? AddTrackRequested;
    public event Action? AddToTrackRequested;
    public event Action? SplitRequested;
    public event Action? DuplicateRequested;
    public event Action? RemoveClipRequested;
    public event Action? NormalizeRequested;
    public event Action? ReverseRequested;
    public event Action? UndoRequested;
    public event Action? RedoRequested;
    public event Action? ExportRequested;
    public event Action? PlayPauseRequested;
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
