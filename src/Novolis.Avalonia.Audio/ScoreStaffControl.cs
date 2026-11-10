using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Novolis.Audio.Midi;

namespace Novolis.Avalonia.Audio;

/// <summary>Orchestral multi-part score preview: one staff (or grand) per track, braced systems.</summary>
public sealed partial class ScoreStaffControl : Control
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
