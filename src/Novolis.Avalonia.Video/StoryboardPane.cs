using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Novolis.Video.Edit;

namespace Novolis.Avalonia.Video;

/// <summary>Scrollable storyboard pane hosting a <see cref="StoryboardStrip"/>.</summary>
public sealed class StoryboardPane : MovieEditPane
{
    /// <summary>Creates a storyboard pane with a default strip.</summary>
    public StoryboardPane()
    {
        Title = "Storyboard";
        Strip = new StoryboardStrip { MinHeight = 80, PixelsPerSecond = 56 };
        Body = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = Strip,
        };
    }

    /// <summary>Gets the inner strip control.</summary>
    public StoryboardStrip Strip { get; }

    /// <summary>Binds the strip to <paramref name="project"/>.</summary>
    public void Bind(MovieProject project) => Strip.Bind(project);

    /// <summary>Updates the playhead marker.</summary>
    public void SetPlayhead(TimeSpan position) => Strip.SetPlayhead(position);

    /// <summary>Highlights the selected clip.</summary>
    public void SetSelectedClip(Guid? clipId) => Strip.SetSelectedClip(clipId);
}
