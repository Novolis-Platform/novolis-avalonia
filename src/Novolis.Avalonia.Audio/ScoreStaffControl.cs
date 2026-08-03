using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Novolis.Audio.Midi;

namespace Novolis.Avalonia.Audio;

/// <summary>Orchestral multi-part score preview: one staff (or grand) per track, braced systems.</summary>
public sealed class ScoreStaffControl : Control
{
    MusicScore? _score;
    const int BarsPerSystem = 4;
    const double StaffSpacing = 8;
    const double PartGap = 18;
    const double SystemGap = 28;
    const double LeftMargin = 88;

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
            return new Size(600, 220);
        var systems = Math.Max(1, (int)Math.Ceiling(_score.BarCount / (double)BarsPerSystem));
        var partH = SystemContentHeight(_score);
        return new Size(Math.Max(640, availableSize.Width), systems * (partH + SystemGap) + 24);
    }

    public override void Render(DrawingContext context)
    {
        if (_score is null)
            return;

        var width = Bounds.Width > 0 ? Bounds.Width : 800;
        context.FillRectangle(new SolidColorBrush(Color.FromRgb(24, 28, 34)), new Rect(0, 0, width, Bounds.Height));

        var partH = SystemContentHeight(_score);
        for (var bar = 0; bar < _score.BarCount; bar += BarsPerSystem)
        {
            var end = Math.Min(_score.BarCount, bar + BarsPerSystem);
            var systemIndex = bar / BarsPerSystem;
            DrawSystem(context, _score, bar, end, yOffset: 12 + systemIndex * (partH + SystemGap), width, partH);
        }
    }

    static double SystemContentHeight(MusicScore score)
    {
        if (score.Tracks.Count == 0)
            return 140;
        double h = 8;
        foreach (var t in score.Tracks)
            h += StaffBlockHeight(t.Clef) + PartGap;
        return h;
    }

    static double StaffBlockHeight(ScoreClef clef) =>
        clef is ScoreClef.Grand ? 4 * StaffSpacing + 28 + 4 * StaffSpacing : 4 * StaffSpacing + 8;

    static void DrawSystem(
        DrawingContext context,
        MusicScore score,
        int barStart,
        int barEnd,
        double yOffset,
        double width,
        double contentHeight)
    {
        var right = width - 12;
        var bars = Math.Max(1, barEnd - barStart);
        var beatWidth = (right - LeftMargin) / (bars * score.BeatsPerBar);
        var lineBrush = new SolidColorBrush(Color.FromRgb(210, 218, 228));
        var pen = new Pen(lineBrush, 1.05);
        var thick = new Pen(lineBrush, 1.6);
        var dim = new SolidColorBrush(Color.FromRgb(150, 160, 175));
        var braceBrush = new SolidColorBrush(Color.FromRgb(180, 190, 205));

        var tracks = score.Tracks.Count > 0
            ? score.Tracks.ToList()
            : [new ScoreTrack("Piano", "keys.grand-soft", clef: ScoreClef.Grand)];

        // System brace
        var topY = yOffset;
        var bottomY = yOffset + contentHeight - PartGap;
        context.DrawLine(new Pen(braceBrush, 3.2), new Point(18, topY + 4), new Point(18, bottomY));
        context.DrawLine(new Pen(braceBrush, 1.2), new Point(24, topY + 4), new Point(24, bottomY));

        var staffTops = new List<(ScoreTrack Track, double TrebleTop, double? BassTop)>();
        var cursor = yOffset + 4;
        foreach (var track in tracks)
        {
            var block = StaffBlockHeight(track.Clef);
            if (track.Clef is ScoreClef.Grand)
            {
                var trebleTop = cursor;
                var bassTop = cursor + 4 * StaffSpacing + 22;
                DrawStaffLines(context, LeftMargin, right, trebleTop, pen);
                DrawStaffLines(context, LeftMargin, right, bassTop, pen);
                context.DrawLine(thick, new Point(LeftMargin, trebleTop), new Point(LeftMargin, bassTop + 4 * StaffSpacing));
                DrawClefLabel(context, track.Clef, LeftMargin - 28, trebleTop + 10, lineBrush);
                DrawClefLabel(context, ScoreClef.Bass, LeftMargin - 28, bassTop + 10, lineBrush);
                staffTops.Add((track, trebleTop, bassTop));
            }
            else
            {
                DrawStaffLines(context, LeftMargin, right, cursor, pen);
                context.DrawLine(thick, new Point(LeftMargin, cursor), new Point(LeftMargin, cursor + 4 * StaffSpacing));
                DrawClefLabel(context, track.Clef, LeftMargin - 28, cursor + 10, lineBrush);
                staffTops.Add((track, cursor, null));
            }

            var (r, g, b) = ScoreTrackColors.Rgb(track.ColorIndex);
            context.DrawText(
                new FormattedText(
                    track.Name,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI Semibold"),
                    11,
                    new SolidColorBrush(Color.FromRgb(r, g, b))),
                new Point(28, cursor - 2));

            cursor += block + PartGap;
        }

        // Barlines through full system
        for (var b = 0; b <= bars; b++)
        {
            var x = LeftMargin + b * score.BeatsPerBar * beatWidth;
            context.DrawLine(pen, new Point(x, topY + 2), new Point(x, bottomY));
        }

        for (var b = barStart; b < barEnd; b++)
        {
            var x = LeftMargin + (b - barStart) * score.BeatsPerBar * beatWidth + 4;
            context.DrawText(
                new FormattedText(
                    $"{b + 1}",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    10,
                    dim),
                new Point(x, topY - 12));
        }

        // Meter / tempo once per system
        context.DrawText(
            new FormattedText(
                $"{score.BeatsPerBar}/{score.BeatUnit}  ·  ♩={score.TempoBpm:0}",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                10,
                dim),
            new Point(LeftMargin + 4, topY - 12));

        var startBeat = barStart * score.BeatsPerBar;
        var endBeat = barEnd * score.BeatsPerBar;
        foreach (var note in score.Notes.Where(n => n.StartBeat < endBeat && n.EndBeat > startBeat))
        {
            var track = score.FindTrack(note.TrackId) ?? tracks[0];
            var slot = staffTops.FirstOrDefault(s => s.Track.Id == track.Id);
            if (slot.Track is null)
                continue;

            var local = note.StartBeat - startBeat;
            var x = LeftMargin + local * beatWidth + 6;
            var useBass = ScoreNotation.UseBassStaff(track.Clef, note.MidiNumber);
            var staffTop = useBass && slot.BassTop is { } bt ? bt : slot.TrebleTop;
            var clef = track.Clef is ScoreClef.Grand
                ? (useBass ? ScoreClef.Bass : ScoreClef.Treble)
                : track.Clef;
            var steps = ScoreNotation.StaffYSteps(note.MidiNumber, clef, useBass);
            var y = staffTop + steps * (StaffSpacing / 2);
            var (nr, ng, nb) = ScoreTrackColors.Rgb(track.ColorIndex);
            var noteBrush = new SolidColorBrush(Color.FromRgb(nr, ng, nb));
            DrawLedgers(context, x + 5, y, staffTop, pen);
            context.DrawEllipse(noteBrush, null, new Rect(x, y - 3.5, 10, 7));
            if (ScoreNotation.NoteValue(note.DurationBeats) != ScoreNoteValue.Whole)
                context.DrawLine(new Pen(noteBrush, 1.2), new Point(x + 10, y), new Point(x + 10, y - 18));
        }
    }

    static void DrawStaffLines(DrawingContext context, double left, double right, double top, Pen pen)
    {
        for (var i = 0; i < 5; i++)
        {
            var y = top + i * StaffSpacing;
            context.DrawLine(pen, new Point(left, y), new Point(right, y));
        }
    }

    static void DrawClefLabel(DrawingContext context, ScoreClef clef, double x, double y, IBrush brush)
    {
        context.DrawText(
            new FormattedText(
                ScoreNotation.ClefAscii(clef),
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI Semibold"),
                15,
                brush),
            new Point(x, y));
    }

    static void DrawLedgers(DrawingContext context, double cx, double y, double staffTop, Pen pen)
    {
        var bottom = staffTop + 4 * StaffSpacing;
        if (y < staffTop - 1)
        {
            for (var ly = staffTop - StaffSpacing; ly >= y - 1; ly -= StaffSpacing)
                context.DrawLine(pen, new Point(cx - 7, ly), new Point(cx + 7, ly));
        }
        else if (y > bottom + 1)
        {
            for (var ly = bottom + StaffSpacing; ly <= y + 1; ly += StaffSpacing)
                context.DrawLine(pen, new Point(cx - 7, ly), new Point(cx + 7, ly));
        }
    }
}
