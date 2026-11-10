using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Novolis.Audio.Edit;

namespace Novolis.Avalonia.Audio;

/// <summary>Multi-track Magix-style arrangement: tools, cross-track drag, mute/solo, waveforms.</summary>
public sealed partial class ArrangementTimelineControl : Control
{
    const double HeaderWidth = 120;
    const double RulerHeight = 22;
    const double MuteSoloHeight = 22;

    MusicProject? _project;
    TimeSpan _playhead;
    Guid? _selectedClipId;
    Guid? _selectedTrackId;
    TimeSpan? _selectionStart;
    TimeSpan? _selectionEnd;
    readonly Dictionary<Guid, float[]> _peakCache = [];
    PointerGesture? _gesture;

    public ArrangementTool Tool { get; set; } = ArrangementTool.Select;

    /// <summary>Pixels per second horizontal scale.</summary>
    public static readonly StyledProperty<double> PixelsPerSecondProperty =
        AvaloniaProperty.Register<ArrangementTimelineControl, double>(nameof(PixelsPerSecond), 80);

    public double PixelsPerSecond
    {
        get => GetValue(PixelsPerSecondProperty);
        set => SetValue(PixelsPerSecondProperty, value);
    }

    public double TrackHeight { get; set; } = 72;

    public event Action<TimeSpan>? SeekRequested;
    public event Action<Guid>? ClipSelected;
    public event Action<Guid>? TrackSelected;
    public event Action? ArrangementChanged;
    public event Action? BeforeMutation;
    public event Action<TimeSpan>? SplitAtRequested;
    public event Action<Guid>? DeleteClipRequested;
    public event Action<Guid, TimeSpan>? DrawAtRequested;

    public (TimeSpan Start, TimeSpan End)? Selection =>
        _selectionStart is { } a && _selectionEnd is { } b
            ? (a < b ? (a, b) : (b, a))
            : null;

    public void Bind(MusicProject project)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _peakCache.Clear();
        foreach (var asset in project.Assets)
            _peakCache[asset.Id] = WaveformPeaks.Extract(asset.Pcm, 128);
        InvalidateVisual();
        InvalidateMeasure();
    }

    public void SetPlayhead(TimeSpan position)
    {
        _playhead = position;
        InvalidateVisual();
    }

    public void SetSelectedClip(Guid? clipId)
    {
        _selectedClipId = clipId;
        InvalidateVisual();
    }

    public void SetSelectedTrack(Guid? trackId)
    {
        _selectedTrackId = trackId;
        InvalidateVisual();
    }

    public void ClearSelection()
    {
        _selectionStart = _selectionEnd = null;
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var duration = _project is null ? TimeSpan.FromSeconds(8) : ArrangementQuery.TotalDuration(_project);
        var seconds = Math.Max(8, duration.TotalSeconds + 2);
        var width = HeaderWidth + seconds * PixelsPerSecond;
        if (double.IsFinite(availableSize.Width))
            width = Math.Max(availableSize.Width, width);
        var tracks = _project?.Tracks.Count ?? 1;
        var height = Math.Max(TrackHeight, tracks * TrackHeight + RulerHeight + 8);
        return new Size(width, height);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_gesture is { Kind: GestureKind.MoveClip or GestureKind.TrimStart or GestureKind.TrimEnd })
            ArrangementChanged?.Invoke();
        _gesture = null;
        e.Pointer.Capture(null);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = Bounds;
        context.FillRectangle(AudioEditPalette.Pane, new Rect(bounds.Size));
        if (_project is null)
            return;

        DrawRuler(context, bounds.Width);

        if (Selection is { } sel)
        {
            var x0 = HeaderWidth + sel.Start.TotalSeconds * PixelsPerSecond;
            var x1 = HeaderWidth + sel.End.TotalSeconds * PixelsPerSecond;
            context.FillRectangle(
                new SolidColorBrush(Color.FromArgb(50, 255, 200, 80)),
                new Rect(Math.Min(x0, x1), RulerHeight, Math.Abs(x1 - x0), bounds.Height - RulerHeight));
        }

        for (var t = 0; t < _project.Tracks.Count; t++)
        {
            var track = _project.Tracks[t];
            var y = RulerHeight + t * TrackHeight;
            var selected = track.Id == _selectedTrackId;
            context.FillRectangle(
                new SolidColorBrush(selected ? Color.FromRgb(36, 48, 58) : Color.FromRgb(30, 36, 44)),
                new Rect(0, y, bounds.Width, TrackHeight - 2));

            context.FillRectangle(
                new SolidColorBrush(selected ? Color.FromRgb(48, 62, 74) : Color.FromRgb(38, 44, 54)),
                new Rect(0, y, HeaderWidth - 2, TrackHeight - 2));
            if (selected)
                context.DrawRectangle(null, new Pen(AudioEditPalette.Amber, 1.5), new Rect(1, y + 1, HeaderWidth - 4, TrackHeight - 4));

            DrawHeaderButton(context, 4, y + 2, 52, 18, track.Mute ? "M" : "m", track.Mute);
            DrawHeaderButton(context, 60, y + 2, 52, 18, track.Solo ? "S" : "s", track.Solo);
            var name = new FormattedText(
                track.Name,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI Semibold"),
                12,
                Brushes.White);
            context.DrawText(name, new Point(8, y + 28));
            var hint = new FormattedText(
                "click = target",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                9,
                new SolidColorBrush(Color.FromRgb(140, 150, 160)));
            context.DrawText(hint, new Point(8, y + 46));

            foreach (var clip in track.Clips)
            {
                var x = HeaderWidth + clip.TimelineStart.TotalSeconds * PixelsPerSecond;
                var w = Math.Max(8, clip.Duration.TotalSeconds * PixelsPerSecond - 2);
                var rect = new Rect(x, y + 8, w, TrackHeight - 16);
                var muted = track.Mute || (_project.Tracks.Any(tr => tr.Solo) && !track.Solo);
                context.FillRectangle(
                    new SolidColorBrush(muted ? Color.FromRgb(55, 60, 68) : Color.FromRgb(40, 90, 100)),
                    rect);
                if (_selectedClipId == clip.Id)
                    context.DrawRectangle(null, new Pen(AudioEditPalette.Amber, 1.5), rect);

                if (Tool is ArrangementTool.Select)
                {
                    context.FillRectangle(AudioEditPalette.Amber, new Rect(rect.X, rect.Y, 3, rect.Height));
                    context.FillRectangle(AudioEditPalette.Amber, new Rect(rect.Right - 3, rect.Y, 3, rect.Height));
                }

                if (_peakCache.TryGetValue(clip.AssetId, out var peaks))
                    DrawPeaks(context, peaks, rect.Deflate(4));

                var asset = _project.FindAsset(clip.AssetId);
                var label = new FormattedText(
                    asset?.Name ?? "?",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    10,
                    Brushes.White);
                context.DrawText(label, new Point(x + 6, y + 12));
            }
        }

        var px = HeaderWidth + _playhead.TotalSeconds * PixelsPerSecond;
        context.DrawLine(
            new Pen(AudioEditPalette.Amber, 2),
            new Point(px, 0),
            new Point(px, bounds.Height));
    }

    void DrawRuler(DrawingContext context, double width)
    {
        context.FillRectangle(new SolidColorBrush(Color.FromRgb(26, 30, 36)), new Rect(0, 0, width, RulerHeight));
        var pen = new Pen(new SolidColorBrush(Color.FromRgb(90, 100, 112)), 1);
        var maxSec = Math.Max(8, (width - HeaderWidth) / PixelsPerSecond);
        var step = PixelsPerSecond >= 120 ? 0.5 : PixelsPerSecond >= 60 ? 1.0 : 2.0;
        for (var s = 0.0; s <= maxSec + 0.001; s += step)
        {
            var x = HeaderWidth + s * PixelsPerSecond;
            context.DrawLine(pen, new Point(x, RulerHeight - 8), new Point(x, RulerHeight));
            if (Math.Abs(s % 1) < 0.001 || step >= 1)
            {
                var label = new FormattedText(
                    TimeSpan.FromSeconds(s).ToString(@"m\:ss"),
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    9,
                    new SolidColorBrush(Color.FromRgb(160, 170, 180)));
                context.DrawText(label, new Point(x + 2, 2));
            }
        }
    }

    static void DrawHeaderButton(DrawingContext context, double x, double y, double w, double h, string text, bool on)
    {
        context.FillRectangle(
            new SolidColorBrush(on ? Color.FromRgb(180, 120, 40) : Color.FromRgb(50, 58, 68)),
            new Rect(x, y, w, h));
        var ft = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI Semibold"),
            11,
            Brushes.White);
        context.DrawText(ft, new Point(x + (w - 10) / 2, y + 1));
    }

    TimeSpan TimeFromX(double x) =>
        TimeSpan.FromSeconds(Math.Max(0, (x - HeaderWidth) / PixelsPerSecond));

    int TrackIndexAt(double y) => (int)((y - RulerHeight) / TrackHeight);

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

    internal enum GestureKind
    {
        SelectRange,
        MoveClip,
        TrimStart,
        TrimEnd,
    }

    internal sealed class PointerGesture
    {
        public PointerGesture(GestureKind kind, Guid? clipId, Guid? trackId, TimeSpan anchorTime, Point origin)
        {
            Kind = kind;
            ClipId = clipId;
            OriginTrackId = trackId;
            AnchorTime = anchorTime;
            Origin = origin;
        }

        public GestureKind Kind { get; }
        public Guid? ClipId { get; }
        public TimeSpan AnchorTime { get; }
        public Point Origin { get; }
        public TimeSpan? OriginStart { get; set; }
        public TimeSpan? OriginDuration { get; set; }
        public TimeSpan? OriginOffset { get; set; }
        public Guid? OriginTrackId { get; set; }
    }
}
