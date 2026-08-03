using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Novolis.Audio.Midi;

namespace Novolis.Avalonia.Audio;

/// <summary>Interactive piano-roll editor bound to a <see cref="MusicScore"/>.</summary>
public sealed class PianoRollControl : Control
{
    const int LowMidi = 36;
    const int HighMidi = 96;
    const double LaneHeight = 14;
    const double BeatWidth = 28;
    const double LabelWidth = 40;

    MusicScore? _score;
    Guid? _selectedId;

    public event Action? ScoreEdited;
    public event Action<Guid?>? SelectionChanged;
    public event Action<int>? PreviewNote;

    public void Bind(MusicScore score)
    {
        _score = score ?? throw new ArgumentNullException(nameof(score));
        score.Changed += () => InvalidateVisual();
        InvalidateVisual();
    }

    public void SetSelected(Guid? id)
    {
        _selectedId = id;
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_score is null)
            return new Size(400, 200);
        var rows = HighMidi - LowMidi + 1;
        var width = LabelWidth + _score.TotalBeats * BeatWidth + 40;
        return new Size(width, rows * LaneHeight + 24);
    }

    public override void Render(DrawingContext context)
    {
        if (_score is null)
            return;

        var rows = HighMidi - LowMidi + 1;
        var gridWidth = _score.TotalBeats * BeatWidth;
        var gridHeight = rows * LaneHeight;

        context.FillRectangle(AudioEditPalette.PaneAlt, new Rect(0, 0, LabelWidth + gridWidth + 8, gridHeight + 24));

        for (var midi = LowMidi; midi <= HighMidi; midi++)
        {
            var y = (HighMidi - midi) * LaneHeight;
            var lane = IsBlack(midi)
                ? new SolidColorBrush(Color.FromRgb(32, 38, 48))
                : new SolidColorBrush(Color.FromRgb(44, 52, 64));
            context.FillRectangle(lane, new Rect(LabelWidth, y, gridWidth, LaneHeight));
            if (midi % 12 == 0)
            {
                context.DrawText(
                    new FormattedText(
                        ScoreNotation.Name(midi),
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"),
                        10,
                        Brushes.White),
                    new Point(4, y + 1));
            }
        }

        for (var beat = 0; beat <= _score.TotalBeats; beat++)
        {
            var x = LabelWidth + beat * BeatWidth;
            var pen = beat % _score.BeatsPerBar == 0
                ? new Pen(AudioEditPalette.Amber, 1.4)
                : new Pen(AudioEditPalette.Border, 1);
            context.DrawLine(pen, new Point(x, 0), new Point(x, gridHeight));
        }

        foreach (var note in _score.Notes)
        {
            if (note.MidiNumber < LowMidi || note.MidiNumber > HighMidi)
                continue;
            var x = LabelWidth + note.StartBeat * BeatWidth;
            var y = (HighMidi - note.MidiNumber) * LaneHeight + 1;
            var w = Math.Max(6, note.DurationBeats * BeatWidth - 2);
            var selected = note.Id == _selectedId;
            var brush = selected ? AudioEditPalette.Amber : AudioEditPalette.Accent;
            context.FillRectangle(brush, new Rect(x, y, w, LaneHeight - 2), 3);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_score is null)
            return;

        var p = e.GetPosition(this);
        if (p.X < LabelWidth)
            return;

        var beat = _score.Snap((p.X - LabelWidth) / BeatWidth);
        var midi = HighMidi - (int)(p.Y / LaneHeight);
        if (midi is < LowMidi or > HighMidi)
            return;

        var props = e.GetCurrentPoint(this).Properties;
        var hit = _score.HitTest(beat, midi, beatSlop: 0.2);
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
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        var placed = _score.Place(midi, beat);
        _selectedId = placed.Id;
        SelectionChanged?.Invoke(placed.Id);
        PreviewNote?.Invoke(midi);
        ScoreEdited?.Invoke();
        InvalidateMeasure();
        InvalidateVisual();
        e.Handled = true;
    }

    static bool IsBlack(int midi)
    {
        var pc = midi % 12;
        return pc is 1 or 3 or 6 or 8 or 10;
    }
}
