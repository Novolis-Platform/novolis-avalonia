using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Audio.Edit;

namespace Novolis.Avalonia.Audio;

/// <summary>Gain / fade inspector for the selected arrangement clip (Audacity Effect lite).</summary>
public sealed class ClipInspectorControl : AudioEditPane
{
    readonly TextBlock _label = new() { Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 8) };
    readonly Slider _gain = new() { Minimum = 0, Maximum = 2, Value = 1, TickFrequency = 0.05, IsSnapToTickEnabled = true };
    readonly Slider _fadeIn = new() { Minimum = 0, Maximum = 2, Value = 0, TickFrequency = 0.05, IsSnapToTickEnabled = true };
    readonly Slider _fadeOut = new() { Minimum = 0, Maximum = 2, Value = 0, TickFrequency = 0.05, IsSnapToTickEnabled = true };
    readonly TextBlock _gainLabel = new() { Foreground = Brushes.White, FontSize = 12 };
    readonly TextBlock _fadeInLabel = new() { Foreground = Brushes.White, FontSize = 12 };
    readonly TextBlock _fadeOutLabel = new() { Foreground = Brushes.White, FontSize = 12 };
    ArrangementClip? _clip;

    /// <summary>Creates the inspector.</summary>
    public ClipInspectorControl()
    {
        Title = "Clip";
        _gain.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty)
                _gainLabel.Text = $"Gain: {_gain.Value:0.00}";
        };
        _fadeIn.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty)
                _fadeInLabel.Text = $"Fade in: {_fadeIn.Value:0.00}s";
        };
        _fadeOut.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty)
                _fadeOutLabel.Text = $"Fade out: {_fadeOut.Value:0.00}s";
        };
        _gainLabel.Text = "Gain: 1.00";
        _fadeInLabel.Text = "Fade in: 0.00s";
        _fadeOutLabel.Text = "Fade out: 0.00s";

        var apply = new Button
        {
            Content = "Apply envelope",
            Margin = new Thickness(0, 8, 0, 0),
            Padding = new Thickness(10, 8),
            Background = AudioEditPalette.Amber,
            Foreground = Brushes.Black,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        apply.Click += (_, _) =>
        {
            if (_clip is null)
                return;
            EnvelopeApplying?.Invoke();
            AudioEditOps.SetClipEnvelope(
                _clip,
                (float)_gain.Value,
                TimeSpan.FromSeconds(_fadeIn.Value),
                TimeSpan.FromSeconds(_fadeOut.Value));
            EnvelopeApplied?.Invoke();
        };

        Body = new StackPanel
        {
            Children =
            {
                _label,
                _gainLabel,
                _gain,
                _fadeInLabel,
                _fadeIn,
                _fadeOutLabel,
                _fadeOut,
                apply,
            },
        };
        IsEnabled = false;
        _label.Text = "Select a clip on the timeline.";
    }

    /// <summary>Raised before Apply (for undo capture).</summary>
    public event Action? EnvelopeApplying;

    /// <summary>Raised after Apply.</summary>
    public event Action? EnvelopeApplied;

    /// <summary>Binds to a clip.</summary>
    public void Bind(MusicProject project, Guid? clipId)
    {
        ArgumentNullException.ThrowIfNull(project);
        _clip = clipId is { } id ? project.FindClip(id) : null;
        if (_clip is null)
        {
            _label.Text = "Select a clip on the timeline.";
            IsEnabled = false;
            return;
        }

        var asset = project.FindAsset(_clip.AssetId);
        _label.Text = $"Clip: {asset?.Name ?? "?"} · {_clip.Duration.TotalSeconds:0.#}s";
        _gain.Value = _clip.Gain;
        _fadeIn.Value = _clip.FadeIn.TotalSeconds;
        _fadeOut.Value = _clip.FadeOut.TotalSeconds;
        IsEnabled = true;
    }
}
