using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Novolis.Video.Edit;

namespace Novolis.Avalonia.Video;

/// <summary>Horizontal Movie Maker–style storyboard of timeline clips.</summary>
public sealed class StoryboardStrip : Control
{
    MovieProject? _project;
    TimeSpan _playhead;
    Guid? _selectedClipId;

    /// <summary>Identifies the <see cref="PixelsPerSecond"/> property.</summary>
    public static readonly StyledProperty<double> PixelsPerSecondProperty =
        AvaloniaProperty.Register<StoryboardStrip, double>(nameof(PixelsPerSecond), 48);

    /// <summary>Horizontal scale of the storyboard.</summary>
    public double PixelsPerSecond
    {
        get => GetValue(PixelsPerSecondProperty);
        set => SetValue(PixelsPerSecondProperty, value);
    }

    /// <summary>Raised when the user clicks a time on the strip.</summary>
    public event Action<TimeSpan>? SeekRequested;

    /// <summary>Raised when a clip under the click is selected.</summary>
    public event Action<Guid>? ClipSelected;

    /// <summary>Binds the strip to a project model.</summary>
    public void Bind(MovieProject project)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        InvalidateVisual();
        InvalidateMeasure();
    }

    /// <summary>Updates the drawn playhead.</summary>
    public void SetPlayhead(TimeSpan position)
    {
        _playhead = position;
        InvalidateVisual();
    }

    /// <summary>Highlights the selected clip, if any.</summary>
    public void SetSelectedClip(Guid? clipId)
    {
        _selectedClipId = clipId;
        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        // ScrollViewer often passes Infinity; never return non-finite sizes to Avalonia.
        var duration = _project is null ? TimeSpan.FromSeconds(10) : StoryboardQuery.TotalDuration(_project);
        var seconds = Math.Max(10, duration.TotalSeconds + 2);
        var contentWidth = seconds * PixelsPerSecond;
        var width = double.IsFinite(availableSize.Width)
            ? Math.Max(availableSize.Width, contentWidth)
            : contentWidth;
        return new Size(width, 72);
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_project is null)
            return;

        var x = e.GetPosition(this).X;
        var time = TimeSpan.FromSeconds(Math.Max(0, x / PixelsPerSecond));
        SeekRequested?.Invoke(time);

        var clip = StoryboardQuery.ClipAt(_project, time);
        if (clip is not null)
        {
            _selectedClipId = clip.Id;
            ClipSelected?.Invoke(clip.Id);
            InvalidateVisual();
        }
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = Bounds;
        context.FillRectangle(new SolidColorBrush(Color.FromRgb(28, 36, 48)), new Rect(bounds.Size));

        if (_project is null)
            return;

        foreach (var clip in _project.Clips)
        {
            var asset = _project.FindAsset(clip.AssetId);
            var x = clip.TimelineStart.TotalSeconds * PixelsPerSecond;
            var w = Math.Max(8, clip.Duration.TotalSeconds * PixelsPerSecond - 2);
            var rect = new Rect(x, 10, w, 44);
            var fill = BrushFor(asset);
            context.FillRectangle(fill, rect);

            if (_selectedClipId == clip.Id)
                context.DrawRectangle(null, new Pen(Brushes.White, 1.5), rect);

            var label = asset?.Name ?? "?";
            var text = new FormattedText(
                label,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                11,
                Brushes.White);
            context.DrawText(text, new Point(x + 6, 24));
        }

        var px = _playhead.TotalSeconds * PixelsPerSecond;
        context.DrawLine(
            new Pen(new SolidColorBrush(Color.FromRgb(255, 180, 60)), 2),
            new Point(px, 0),
            new Point(px, bounds.Height));
    }

    static IBrush BrushFor(MediaAsset? asset)
    {
        if (asset?.Color is { } c)
            return new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));

        return asset?.Kind switch
        {
            MediaKind.Image => new SolidColorBrush(Color.FromRgb(40, 110, 120)),
            MediaKind.Video => new SolidColorBrush(Color.FromRgb(50, 90, 140)),
            MediaKind.Audio => new SolidColorBrush(Color.FromRgb(90, 70, 40)),
            _ => new SolidColorBrush(Color.FromRgb(70, 80, 95)),
        };
    }
}
