using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Novolis.Audio.Edit;

namespace Novolis.Avalonia.Audio;

/// <summary>Multi-track arrangement timeline with waveforms and playhead.</summary>
public sealed class ArrangementTimelineControl : Control
{
    MusicProject? _project;
    TimeSpan _playhead;
    Guid? _selectedClipId;
    readonly Dictionary<Guid, float[]> _peakCache = [];

    /// <summary>Pixels per second horizontal scale.</summary>
    public static readonly StyledProperty<double> PixelsPerSecondProperty =
        AvaloniaProperty.Register<ArrangementTimelineControl, double>(nameof(PixelsPerSecond), 80);

    /// <summary>Gets or sets horizontal zoom.</summary>
    public double PixelsPerSecond
    {
        get => GetValue(PixelsPerSecondProperty);
        set => SetValue(PixelsPerSecondProperty, value);
    }

    /// <summary>Track lane height.</summary>
    public double TrackHeight { get; set; } = 64;

    /// <summary>Raised on scrub.</summary>
    public event Action<TimeSpan>? SeekRequested;

    /// <summary>Raised when a clip is selected.</summary>
    public event Action<Guid>? ClipSelected;

    /// <summary>Binds the arrangement.</summary>
    public void Bind(MusicProject project)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _peakCache.Clear();
        foreach (var asset in project.Assets)
            _peakCache[asset.Id] = WaveformPeaks.Extract(asset.Pcm, 96);
        InvalidateVisual();
        InvalidateMeasure();
    }

    /// <summary>Updates playhead.</summary>
    public void SetPlayhead(TimeSpan position)
    {
        _playhead = position;
        InvalidateVisual();
    }

    /// <summary>Highlights a clip.</summary>
    public void SetSelectedClip(Guid? clipId)
    {
        _selectedClipId = clipId;
        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var duration = _project is null ? TimeSpan.FromSeconds(8) : ArrangementQuery.TotalDuration(_project);
        var seconds = Math.Max(8, duration.TotalSeconds + 2);
        var width = seconds * PixelsPerSecond;
        if (double.IsFinite(availableSize.Width))
            width = Math.Max(availableSize.Width, width);
        var tracks = _project?.Tracks.Count ?? 1;
        var height = Math.Max(TrackHeight, tracks * TrackHeight + 8);
        return new Size(width, height);
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_project is null)
            return;

        var pos = e.GetPosition(this);
        var time = TimeSpan.FromSeconds(Math.Max(0, pos.X / PixelsPerSecond));
        SeekRequested?.Invoke(time);

        var trackIndex = (int)(pos.Y / TrackHeight);
        if (trackIndex >= 0 && trackIndex < _project.Tracks.Count)
        {
            var track = _project.Tracks[trackIndex];
            foreach (var clip in track.Clips)
            {
                if (clip.Contains(time))
                {
                    _selectedClipId = clip.Id;
                    ClipSelected?.Invoke(clip.Id);
                    InvalidateVisual();
                    break;
                }
            }
        }
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = Bounds;
        context.FillRectangle(AudioEditPalette.Pane, new Rect(bounds.Size));
        if (_project is null)
            return;

        for (var t = 0; t < _project.Tracks.Count; t++)
        {
            var track = _project.Tracks[t];
            var y = t * TrackHeight;
            context.FillRectangle(
                new SolidColorBrush(Color.FromRgb(30, 36, 44)),
                new Rect(0, y, bounds.Width, TrackHeight - 2));

            var name = new FormattedText(
                track.Name + (track.Mute ? " (mute)" : ""),
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                11,
                Brushes.White);
            context.DrawText(name, new Point(4, y + 4));

            foreach (var clip in track.Clips)
            {
                var x = clip.TimelineStart.TotalSeconds * PixelsPerSecond;
                var w = Math.Max(8, clip.Duration.TotalSeconds * PixelsPerSecond - 2);
                var rect = new Rect(x, y + 18, w, TrackHeight - 26);
                context.FillRectangle(new SolidColorBrush(Color.FromRgb(40, 70, 80)), rect);
                if (_selectedClipId == clip.Id)
                    context.DrawRectangle(null, new Pen(AudioEditPalette.Amber, 1.5), rect);

                if (_peakCache.TryGetValue(clip.AssetId, out var peaks))
                    DrawPeaks(context, peaks, rect);

                var asset = _project.FindAsset(clip.AssetId);
                var label = new FormattedText(
                    asset?.Name ?? "?",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    10,
                    Brushes.White);
                context.DrawText(label, new Point(x + 4, y + 20));
            }
        }

        var px = _playhead.TotalSeconds * PixelsPerSecond;
        context.DrawLine(
            new Pen(AudioEditPalette.Amber, 2),
            new Point(px, 0),
            new Point(px, bounds.Height));
    }

    static void DrawPeaks(DrawingContext context, float[] peaks, Rect rect)
    {
        var buckets = peaks.Length / 2;
        var mid = rect.Y + rect.Height * 0.5;
        var pen = new Pen(AudioEditPalette.Wave, 1);
        for (var i = 0; i < buckets; i++)
        {
            var x = rect.X + i / (double)buckets * rect.Width;
            var y0 = mid - peaks[i * 2 + 1] * rect.Height * 0.4;
            var y1 = mid - peaks[i * 2] * rect.Height * 0.4;
            context.DrawLine(pen, new Point(x, y0), new Point(x, y1));
        }
    }
}
