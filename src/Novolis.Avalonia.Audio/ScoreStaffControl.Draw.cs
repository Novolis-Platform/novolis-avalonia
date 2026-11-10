using Avalonia;
using Avalonia.Media;
using Novolis.Audio.Midi;

namespace Novolis.Avalonia.Audio;

public sealed partial class ScoreStaffControl
{
    readonly record struct StaffLayoutSlot(ScoreTrack Track, double TrebleTop, double? BassTop);

    readonly record struct SystemDrawContext(
        Pen Pen,
        Pen ThickPen,
        IBrush DimBrush,
        double Right,
        double BeatWidth,
        double TopY,
        double BottomY);

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

        var topY = yOffset;
        var bottomY = yOffset + contentHeight - PartGap;
        DrawSystemBrace(context, braceBrush, topY, bottomY);

        var staffTops = DrawSystemStaves(context, tracks, yOffset, right, pen, thick, lineBrush);
        var draw = new SystemDrawContext(pen, thick, dim, right, beatWidth, topY, bottomY);
        DrawSystemBarlines(context, score, barStart, barEnd, bars, draw);
        DrawSystemNotes(context, score, tracks, staffTops, barStart, barEnd, pen, beatWidth);
    }

    static void DrawSystemBrace(DrawingContext context, IBrush braceBrush, double topY, double bottomY)
    {
        context.DrawLine(new Pen(braceBrush, 3.2), new Point(18, topY + 4), new Point(18, bottomY));
        context.DrawLine(new Pen(braceBrush, 1.2), new Point(24, topY + 4), new Point(24, bottomY));
    }

    static List<StaffLayoutSlot> DrawSystemStaves(
        DrawingContext context,
        IReadOnlyList<ScoreTrack> tracks,
        double yOffset,
        double right,
        Pen pen,
        Pen thick,
        IBrush lineBrush)
    {
        var staffTops = new List<StaffLayoutSlot>();
        var cursor = yOffset + 4;
        foreach (var track in tracks)
        {
            var block = StaffBlockHeight(track.Clef);
            if (track.Clef is ScoreClef.Grand)
                staffTops.Add(DrawGrandStaffBlock(context, track, ref cursor, right, pen, thick, lineBrush));
            else
                staffTops.Add(DrawSingleStaffBlock(context, track, ref cursor, right, pen, thick, lineBrush));
            cursor += block + PartGap;
        }

        return staffTops;
    }

    static StaffLayoutSlot DrawGrandStaffBlock(
        DrawingContext context,
        ScoreTrack track,
        ref double cursor,
        double right,
        Pen pen,
        Pen thick,
        IBrush lineBrush)
    {
        var trebleTop = cursor;
        var bassTop = cursor + 4 * StaffSpacing + 22;
        DrawStaffLines(context, LeftMargin, right, trebleTop, pen);
        DrawStaffLines(context, LeftMargin, right, bassTop, pen);
        context.DrawLine(thick, new Point(LeftMargin, trebleTop), new Point(LeftMargin, bassTop + 4 * StaffSpacing));
        DrawClefLabel(context, track.Clef, LeftMargin - 28, trebleTop + 10, lineBrush);
        DrawClefLabel(context, ScoreClef.Bass, LeftMargin - 28, bassTop + 10, lineBrush);
        DrawPartLabel(context, track, cursor);
        return new StaffLayoutSlot(track, trebleTop, bassTop);
    }

    static StaffLayoutSlot DrawSingleStaffBlock(
        DrawingContext context,
        ScoreTrack track,
        ref double cursor,
        double right,
        Pen pen,
        Pen thick,
        IBrush lineBrush)
    {
        DrawStaffLines(context, LeftMargin, right, cursor, pen);
        context.DrawLine(thick, new Point(LeftMargin, cursor), new Point(LeftMargin, cursor + 4 * StaffSpacing));
        DrawClefLabel(context, track.Clef, LeftMargin - 28, cursor + 10, lineBrush);
        DrawPartLabel(context, track, cursor);
        return new StaffLayoutSlot(track, cursor, null);
    }

    static void DrawPartLabel(DrawingContext context, ScoreTrack track, double cursor)
    {
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
    }

    static void DrawSystemBarlines(
        DrawingContext context,
        MusicScore score,
        int barStart,
        int barEnd,
        int bars,
        SystemDrawContext draw)
    {
        for (var b = 0; b <= bars; b++)
        {
            var x = LeftMargin + b * score.BeatsPerBar * draw.BeatWidth;
            context.DrawLine(draw.Pen, new Point(x, draw.TopY + 2), new Point(x, draw.BottomY));
        }

        for (var b = barStart; b < barEnd; b++)
        {
            var x = LeftMargin + (b - barStart) * score.BeatsPerBar * draw.BeatWidth + 4;
            context.DrawText(
                new FormattedText(
                    $"{b + 1}",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    10,
                    draw.DimBrush),
                new Point(x, draw.TopY - 12));
        }

        context.DrawText(
            new FormattedText(
                $"{score.BeatsPerBar}/{score.BeatUnit}  ·  ♩={score.TempoBpm:0}",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                10,
                draw.DimBrush),
            new Point(LeftMargin + 4, draw.TopY - 12));
    }

    static void DrawSystemNotes(
        DrawingContext context,
        MusicScore score,
        IReadOnlyList<ScoreTrack> tracks,
        IReadOnlyList<StaffLayoutSlot> staffTops,
        int barStart,
        int barEnd,
        Pen pen,
        double beatWidth)
    {
        var startBeat = barStart * score.BeatsPerBar;
        var endBeat = barEnd * score.BeatsPerBar;
        foreach (var note in score.Notes.Where(n => n.StartBeat < endBeat && n.EndBeat > startBeat))
        {
            var track = score.FindTrack(note.TrackId) ?? tracks[0];
            var slot = staffTops.FirstOrDefault(s => s.Track.Id == track.Id);
            if (slot.Track is null)
                continue;

            DrawScoreNote(context, note, track, slot, startBeat, beatWidth, pen);
        }
    }

    static void DrawScoreNote(
        DrawingContext context,
        ScoreNote note,
        ScoreTrack track,
        StaffLayoutSlot slot,
        double startBeat,
        double beatWidth,
        Pen pen)
    {
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
