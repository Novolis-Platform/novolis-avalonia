using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Novolis.Audio.Midi;

namespace Novolis.Avalonia.Audio;

/// <summary>Lightweight grand-staff preview of a <see cref="MusicScore"/>.</summary>
public sealed class ScoreStaffControl : Control
{
    MusicScore? _score;

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

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_score is null)
            return new Size(600, 180);
        var systems = (int)Math.Ceiling(_score.BarCount / 4.0);
        return new Size(Math.Max(600, availableSize.Width), systems * 170 + 20);
    }

    public override void Render(DrawingContext context)
    {
        if (_score is null)
            return;

        var width = Bounds.Width > 0 ? Bounds.Width : 760;
        context.FillRectangle(Brushes.WhiteSmoke, new Rect(0, 0, width, Bounds.Height));

        const int barsPerSystem = 4;
        for (var bar = 0; bar < _score.BarCount; bar += barsPerSystem)
        {
            var end = Math.Min(_score.BarCount, bar + barsPerSystem);
            var systemIndex = bar / barsPerSystem;
            DrawSystem(context, _score, bar, end, yOffset: systemIndex * 170, width);
        }
    }

    static void DrawSystem(DrawingContext context, MusicScore score, int barStart, int barEnd, double yOffset, double width)
    {
        const double left = 48;
        var right = width - 12;
        const double trebleTop = 20;
        const double bassTop = 95;
        const double spacing = 8;
        var bars = Math.Max(1, barEnd - barStart);
        var beatWidth = (right - left) / (bars * score.BeatsPerBar);
        var pen = new Pen(Brushes.Black, 1.1);
        var thick = new Pen(Brushes.Black, 1.5);

        void Staff(double top)
        {
            for (var i = 0; i < 5; i++)
            {
                var y = yOffset + top + i * spacing;
                context.DrawLine(pen, new Point(left, y), new Point(right, y));
            }
        }

        Staff(trebleTop);
        Staff(bassTop);
        context.DrawLine(thick, new Point(left, yOffset + trebleTop), new Point(left, yOffset + bassTop + 4 * spacing));

        for (var b = 0; b <= bars; b++)
        {
            var x = left + b * score.BeatsPerBar * beatWidth;
            context.DrawLine(pen, new Point(x, yOffset + trebleTop), new Point(x, yOffset + bassTop + 4 * spacing));
        }

        for (var b = barStart; b < barEnd; b++)
        {
            var x = left + (b - barStart) * score.BeatsPerBar * beatWidth + 4;
            context.DrawText(
                new FormattedText(
                    $"{b + 1}",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    10,
                    Brushes.DimGray),
                new Point(x, yOffset + trebleTop - 14));
        }

        context.DrawText(
            new FormattedText("G", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI Semibold"), 14, Brushes.Black),
            new Point(12, yOffset + trebleTop + 8));
        context.DrawText(
            new FormattedText("F", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI Semibold"), 14, Brushes.Black),
            new Point(12, yOffset + bassTop + 8));

        var startBeat = barStart * score.BeatsPerBar;
        var endBeat = barEnd * score.BeatsPerBar;
        foreach (var note in score.Notes.Where(n => n.StartBeat < endBeat && n.EndBeat > startBeat))
        {
            var local = note.StartBeat - startBeat;
            var x = left + local * beatWidth + 6;
            var bass = ScoreNotation.PreferBassStaff(note.MidiNumber);
            var staffTop = bass ? bassTop : trebleTop;
            var steps = StaffYSteps(note.MidiNumber, bass);
            var y = yOffset + staffTop + steps * (spacing / 2);
            context.DrawEllipse(Brushes.Black, null, new Rect(x, y - 3.5, 10, 7));
            if (ScoreNotation.NoteValue(note.DurationBeats) != ScoreNoteValue.Whole)
                context.DrawLine(pen, new Point(x + 10, y), new Point(x + 10, y - 18));
        }
    }

    static double StaffYSteps(int midi, bool bass)
    {
        static int WhiteIndex(int m)
        {
            var octave = m / 12;
            var pc = m % 12;
            var white = pc switch
            {
                0 => 0, 1 => 0, 2 => 1, 3 => 1, 4 => 2, 5 => 3, 6 => 3, 7 => 4, 8 => 4, 9 => 5, 10 => 5, 11 => 6,
                _ => 0,
            };
            return octave * 7 + white;
        }

        var topMidi = bass ? 57 : 77;
        return WhiteIndex(topMidi) - WhiteIndex(midi);
    }
}
