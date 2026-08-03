using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Novolis.Avalonia.Video;

/// <summary>Classic Movie Maker tasks list; raises events for host wiring.</summary>
public sealed class MovieEditTasksPane : MovieEditPane
{
    /// <summary>Creates the standard edit task buttons.</summary>
    public MovieEditTasksPane()
    {
        Title = "Tasks";
        Body = new StackPanel
        {
            Children =
            {
                TaskButton("Import pictures…", () => ImportPicturesRequested?.Invoke()),
                TaskButton("Make color card", () => AddColorCardRequested?.Invoke()),
                TaskButton("Add to storyboard", () => AddToStoryboardRequested?.Invoke()),
                TaskButton("Split at playhead", () => SplitAtPlayheadRequested?.Invoke()),
                TaskButton("Remove clip", () => RemoveClipRequested?.Invoke()),
                TaskButton("Play / Pause", () => PlayPauseRequested?.Invoke()),
                TaskButton("Rewind", () => RewindRequested?.Invoke()),
            },
        };
    }

    /// <summary>Raised when Import pictures is clicked.</summary>
    public event Action? ImportPicturesRequested;

    /// <summary>Raised when Make color card is clicked.</summary>
    public event Action? AddColorCardRequested;

    /// <summary>Raised when Add to storyboard is clicked.</summary>
    public event Action? AddToStoryboardRequested;

    /// <summary>Raised when Split at playhead is clicked.</summary>
    public event Action? SplitAtPlayheadRequested;

    /// <summary>Raised when Remove clip is clicked.</summary>
    public event Action? RemoveClipRequested;

    /// <summary>Raised when Play / Pause is clicked.</summary>
    public event Action? PlayPauseRequested;

    /// <summary>Raised when Rewind is clicked.</summary>
    public event Action? RewindRequested;

    static Button TaskButton(string label, Action action)
    {
        var b = new Button
        {
            Content = label,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 0, 0, 8),
            Padding = new Thickness(10, 8),
            Background = MovieEditPalette.Accent,
            Foreground = Brushes.White,
        };
        b.Click += (_, _) => action();
        return b;
    }
}
