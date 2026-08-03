using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Novolis.Audio.Core;
using Novolis.Audio.Edit;

namespace Novolis.Avalonia.Audio;

/// <summary>Audacity-style min/max waveform from a <see cref="PcmBuffer"/>.</summary>
public sealed class WaveformControl : Control
{
    float[]? _peaks;

    /// <summary>Rebuilds peaks from <paramref name="pcm"/>.</summary>
    public void Bind(PcmBuffer? pcm, int buckets = 128)
    {
        _peaks = pcm is null ? null : WaveformPeaks.Extract(pcm, Math.Max(8, buckets));
        InvalidateVisual();
    }

    /// <summary>Binds precomputed interleaved min/max peaks.</summary>
    public void BindPeaks(float[]? peaks)
    {
        _peaks = peaks;
        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var w = double.IsFinite(availableSize.Width) ? availableSize.Width : 200;
        var h = double.IsFinite(availableSize.Height) ? Math.Max(24, availableSize.Height) : 48;
        return new Size(w, h);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = Bounds;
        context.FillRectangle(Brushes.Black, new Rect(bounds.Size));
        if (_peaks is null || _peaks.Length < 2)
            return;

        var buckets = _peaks.Length / 2;
        var mid = bounds.Height * 0.5;
        var pen = new Pen(AudioEditPalette.Wave, 1);
        for (var i = 0; i < buckets; i++)
        {
            var x = i / (double)buckets * bounds.Width;
            var min = _peaks[i * 2];
            var max = _peaks[i * 2 + 1];
            var y0 = mid - max * mid * 0.9;
            var y1 = mid - min * mid * 0.9;
            context.DrawLine(pen, new Point(x, y0), new Point(x, y1));
        }

        context.DrawLine(
            new Pen(new SolidColorBrush(Color.FromRgb(70, 80, 90)), 1),
            new Point(0, mid),
            new Point(bounds.Width, mid));
    }
}
