using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Novolis.Audio.Midi;
using Novolis.Audio.MusicTheory;

namespace Novolis.Avalonia.Audio;

/// <summary>Professional piano-roll editor: keyboard lane, bar ruler, drag/resize, chord paint.</summary>
public sealed class PianoRollControl : Control
{
    const int LowMidi = 36;  // C2
    const int HighMidi = 96; // C7
    const double HeaderHeight = 28;
    const double KeyWidth = 58;
    const double ResizeHandle = 8;

    double _laneHeight = 16;
    double _beatWidth = 42;
    MusicScore? _score;
    Guid? _selectedId;
    double? _playheadBeat;
    DragMode _drag;
    ScoreNote? _dragNote;
    Point _dragOrigin;
    double _originBeat;
    double _originDur;
    int _originMidi;

    enum DragMode { None, Move, Resize }

    public event Action? ScoreEdited;
    public event Action<Guid?>? SelectionChanged;
    public event Action<int>? PreviewNote;

    /// <summary>When true, Ctrl+click paints a seventh chord from the clicked root.</summary>
    public bool ChordPaintEnabled { get; set; } = true;

    public ChordQuality ChordPaintQuality { get; set; } = ChordQuality.MajorSeventh;

    public void Bind(MusicScore score)
    {
        _score = score ?? throw new ArgumentNullException(nameof(score));
        score.Changed += () =>
        {
            InvalidateMeasure();
            InvalidateVisual();
        };
        InvalidateMeasure();
        InvalidateVisual();
    }

    public void SetSelected(Guid? id)
    {
        _selectedId = id;
        InvalidateVisual();
    }

    /// <summary>Sets the playback cursor in beats, or null to hide.</summary>
    public void SetPlayhead(double? beat)
    {
        _playheadBeat = beat;
        InvalidateVisual();
    }

    public void Zoom(double beatFactor, double laneFactor = 1)
    {
        _beatWidth = Math.Clamp(_beatWidth * beatFactor, 18, 96);
        _laneHeight = Math.Clamp(_laneHeight * laneFactor, 10, 28);
        InvalidateMeasure();
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_score is null)
            return new Size(640, 360);
        var rows = HighMidi - LowMidi + 1;
        var width = KeyWidth + _score.TotalBeats * _beatWidth + 24;
        return new Size(width, HeaderHeight + rows * _laneHeight + 4);
    }

    public override void Render(DrawingContext context)
    {
        if (_score is null)
            return;

        var rows = HighMidi - LowMidi + 1;
        var gridWidth = _score.TotalBeats * _beatWidth;
        var gridHeight = rows * _laneHeight;
        var totalW = KeyWidth + gridWidth + 8;
        var totalH = HeaderHeight + gridHeight;

        context.FillRectangle(new SolidColorBrush(Color.FromRgb(18, 22, 28)), new Rect(0, 0, totalW, totalH));

        DrawHeader(context, gridWidth);
        DrawKeyboard(context, gridHeight);
        DrawLanes(context, gridWidth, gridHeight);
        DrawGrid(context, gridWidth, gridHeight);
        DrawNotes(context);
        DrawPlayhead(context, gridHeight);
    }

    void DrawPlayhead(DrawingContext context, double gridHeight)
    {
        if (_playheadBeat is not { } beat || _score is null)
            return;
        var x = KeyWidth + beat * _beatWidth;
        var pen = new Pen(new SolidColorBrush(Color.FromRgb(255, 90, 90)), 2);
        context.DrawLine(pen, new Point(x, HeaderHeight), new Point(x, HeaderHeight + gridHeight));
        context.FillRectangle(
            new SolidColorBrush(Color.FromRgb(255, 90, 90)),
            new Rect(x - 5, HeaderHeight - 8, 10, 8));
    }

    void DrawHeader(DrawingContext context, double gridWidth)
    {
        context.FillRectangle(new SolidColorBrush(Color.FromRgb(28, 34, 42)), new Rect(KeyWidth, 0, gridWidth, HeaderHeight));
        context.DrawLine(new Pen(AudioEditPalette.Border, 1), new Point(KeyWidth, HeaderHeight), new Point(KeyWidth + gridWidth, HeaderHeight));

        if (_score is null)
            return;

        for (var bar = 0; bar < _score.BarCount; bar++)
        {
            var x = KeyWidth + bar * _score.BeatsPerBar * _beatWidth;
            context.DrawText(
                Fmt($"{bar + 1}", 12, AudioEditPalette.Amber, semibold: true),
                new Point(x + 6, 4));

            for (var beat = 0; beat < _score.BeatsPerBar; beat++)
            {
                var bx = x + beat * _beatWidth;
                var h = beat == 0 ? 14 : 8;
                context.DrawLine(
                    new Pen(beat == 0 ? AudioEditPalette.Amber : AudioEditPalette.Border, beat == 0 ? 1.4 : 1),
                    new Point(bx, HeaderHeight - h),
                    new Point(bx, HeaderHeight));
            }
        }
    }

    void DrawKeyboard(DrawingContext context, double gridHeight)
    {
        context.FillRectangle(new SolidColorBrush(Color.FromRgb(24, 28, 34)), new Rect(0, HeaderHeight, KeyWidth, gridHeight));

        for (var midi = LowMidi; midi <= HighMidi; midi++)
        {
            var y = HeaderHeight + (HighMidi - midi) * _laneHeight;
            var black = IsBlack(midi);
            var fill = black
                ? new SolidColorBrush(Color.FromRgb(12, 14, 18))
                : new SolidColorBrush(Color.FromRgb(232, 234, 238));
            context.FillRectangle(fill, new Rect(0, y, KeyWidth - 1, _laneHeight - 0.5));
            if (!black && midi % 12 == 0)
            {
                context.DrawText(
                    Fmt(ScoreNotation.Name(midi), 10, new SolidColorBrush(Color.FromRgb(40, 48, 58))),
                    new Point(6, y + (_laneHeight - 12) / 2));
            }
            else if (black)
            {
                context.FillRectangle(
                    new SolidColorBrush(Color.FromRgb(8, 10, 12)),
                    new Rect(KeyWidth * 0.45, y + 1, KeyWidth * 0.5, _laneHeight - 2));
            }
        }

        context.DrawLine(
            new Pen(AudioEditPalette.Border, 1),
            new Point(KeyWidth, HeaderHeight),
            new Point(KeyWidth, HeaderHeight + gridHeight));
    }

    void DrawLanes(DrawingContext context, double gridWidth, double gridHeight)
    {
        for (var midi = LowMidi; midi <= HighMidi; midi++)
        {
            var y = HeaderHeight + (HighMidi - midi) * _laneHeight;
            var fill = IsBlack(midi)
                ? new SolidColorBrush(Color.FromRgb(26, 30, 38))
                : new SolidColorBrush(Color.FromRgb(34, 40, 50));
            if (midi % 12 == 0)
                fill = new SolidColorBrush(Color.FromRgb(40, 48, 60));
            context.FillRectangle(fill, new Rect(KeyWidth, y, gridWidth, _laneHeight));
        }
    }

    void DrawGrid(DrawingContext context, double gridWidth, double gridHeight)
    {
        if (_score is null)
            return;

        var top = HeaderHeight;
        var bottom = HeaderHeight + gridHeight;
        var snap = Math.Max(0.0625, _score.SnapBeats);

        for (double beat = 0; beat <= _score.TotalBeats + 0.0001; beat += snap)
        {
            var x = KeyWidth + beat * _beatWidth;
            var onBar = Math.Abs(beat % _score.BeatsPerBar) < 0.001;
            var onBeat = Math.Abs(beat % 1.0) < 0.001;
            var pen = onBar
                ? new Pen(new SolidColorBrush(Color.FromRgb(200, 150, 70)), 1.5)
                : onBeat
                    ? new Pen(new SolidColorBrush(Color.FromRgb(70, 82, 98)), 1.1)
                    : new Pen(new SolidColorBrush(Color.FromRgb(48, 56, 68)), 1);
            context.DrawLine(pen, new Point(x, top), new Point(x, bottom));
        }
    }

    void DrawNotes(DrawingContext context)
    {
        if (_score is null)
            return;

        foreach (var note in _score.Notes)
        {
            if (note.MidiNumber < LowMidi || note.MidiNumber > HighMidi)
                continue;

            var rect = NoteRect(note);
            var selected = note.Id == _selectedId;
            var track = _score.FindTrack(note.TrackId);
            var (r, g, b) = ScoreTrackColors.Rgb(track?.ColorIndex ?? 0);
            var t = note.Velocity / 127f;
            IBrush brush = selected
                ? Brushes.White
                : new SolidColorBrush(Color.FromRgb(
                    (byte)Math.Clamp(r * (0.55 + 0.45 * t), 0, 255),
                    (byte)Math.Clamp(g * (0.55 + 0.45 * t), 0, 255),
                    (byte)Math.Clamp(b * (0.55 + 0.45 * t), 0, 255)));
            context.FillRectangle(brush, rect, 3);
            if (selected)
                context.DrawRectangle(new Pen(AudioEditPalette.Amber, 1.6), rect, 3);
            else
                context.DrawRectangle(new Pen(new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)), 1), rect, 3);

            context.FillRectangle(
                selected ? AudioEditPalette.Amber : new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                new Rect(rect.Right - 4, rect.Y + 2, 3, rect.Height - 4));

            if (rect.Width > 28)
            {
                context.DrawText(
                    Fmt(ScoreNotation.Name(note.MidiNumber), 10, selected ? Brushes.Black : Brushes.White),
                    new Point(rect.X + 4, rect.Y + Math.Max(0, (rect.Height - 12) / 2)));
            }
        }
    }

    Rect NoteRect(ScoreNote note)
    {
        var x = KeyWidth + note.StartBeat * _beatWidth;
        var y = HeaderHeight + (HighMidi - note.MidiNumber) * _laneHeight + 1;
        var w = Math.Max(8, note.DurationBeats * _beatWidth - 2);
        return new Rect(x, y, w, _laneHeight - 2);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_score is null)
            return;

        var p = e.GetPosition(this);
        if (p.Y < HeaderHeight || p.X < KeyWidth)
            return;

        var props = e.GetCurrentPoint(this).Properties;
        var hit = HitNote(p);

        if (props.IsRightButtonPressed || e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            if (hit is not null)
            {
                _score.Remove(hit.Id);
                _selectedId = null;
                SelectionChanged?.Invoke(null);
                ScoreEdited?.Invoke();
            }

            e.Handled = true;
            return;
        }

        if (hit is not null)
        {
            _selectedId = hit.Id;
            SelectionChanged?.Invoke(hit.Id);
            PreviewNote?.Invoke(hit.MidiNumber);
            var rect = NoteRect(hit);
            _drag = p.X >= rect.Right - ResizeHandle ? DragMode.Resize : DragMode.Move;
            _dragNote = hit;
            _dragOrigin = p;
            _originBeat = hit.StartBeat;
            _originDur = hit.DurationBeats;
            _originMidi = hit.MidiNumber;
            e.Pointer.Capture(this);
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        var beat = _score.Snap((p.X - KeyWidth) / _beatWidth);
        var midi = MidiAtY(p.Y);
        if (midi is null)
            return;

        if (ChordPaintEnabled && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            _score.PlaceChord(midi.Value, ChordPaintQuality, beat, _score.DefaultDurationBeats * 2);
            PreviewNote?.Invoke(midi.Value);
            ScoreEdited?.Invoke();
            InvalidateMeasure();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        var placed = _score.Place(midi.Value, beat);
        _selectedId = placed.Id;
        SelectionChanged?.Invoke(placed.Id);
        PreviewNote?.Invoke(midi.Value);
        ScoreEdited?.Invoke();
        InvalidateMeasure();
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_score is null || _drag == DragMode.None || _dragNote is null)
            return;

        var p = e.GetPosition(this);
        var dx = p.X - _dragOrigin.X;
        var dy = p.Y - _dragOrigin.Y;

        if (_drag == DragMode.Move)
        {
            var beatDelta = dx / _beatWidth;
            var midiDelta = -(int)Math.Round(dy / _laneHeight);
            _dragNote.StartBeat = Math.Max(0, _score.Snap(_originBeat + beatDelta));
            _dragNote.MidiNumber = Math.Clamp(_originMidi + midiDelta, LowMidi, HighMidi);
        }
        else if (_drag == DragMode.Resize)
        {
            var newDur = Math.Max(_score.SnapBeats, _score.Snap(_originDur + dx / _beatWidth));
            _dragNote.DurationBeats = newDur;
        }

        _score.EnsureBarsFor(_dragNote.EndBeat);
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_drag != DragMode.None)
        {
            _drag = DragMode.None;
            _dragNote = null;
            e.Pointer.Capture(null);
            _score?.NotifyChanged();
            ScoreEdited?.Invoke();
            InvalidateMeasure();
            InvalidateVisual();
            e.Handled = true;
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (_drag == DragMode.None)
            return;
        _drag = DragMode.None;
        _dragNote = null;
        _score?.NotifyChanged();
        ScoreEdited?.Invoke();
    }

    ScoreNote? HitNote(Point p)
    {
        if (_score is null)
            return null;
        for (var i = _score.Notes.Count - 1; i >= 0; i--)
        {
            var n = _score.Notes[i];
            if (n.MidiNumber < LowMidi || n.MidiNumber > HighMidi)
                continue;
            if (NoteRect(n).Contains(p))
                return n;
        }

        return null;
    }

    int? MidiAtY(double y)
    {
        var midi = HighMidi - (int)((y - HeaderHeight) / _laneHeight);
        return midi is >= LowMidi and <= HighMidi ? midi : null;
    }

    static bool IsBlack(int midi)
    {
        var pc = midi % 12;
        return pc is 1 or 3 or 6 or 8 or 10;
    }

    static FormattedText Fmt(string text, double size, IBrush brush, bool semibold = false) =>
        new(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI", FontStyle.Normal, semibold ? FontWeight.SemiBold : FontWeight.Normal),
            size,
            brush);
}
