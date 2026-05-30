using Avalonia;
using Avalonia.Controls;
using Novolis.Audio.Voice.Design;

namespace Novolis.Avalonia.Voice;

/// <summary>Edits ATC phraseology and radio DSP on a <see cref="VoicePresetDraft"/>.</summary>
public sealed class AtcDeliveryInspector : StackPanel
{
    private readonly CheckBox _phraseology = new() { Content = "ICAO phraseology" };
    private readonly CheckBox _radioEffects = new() { Content = "Radio effects (atc-radio)" };
    private readonly Slider _drive;
    private readonly Slider _highPass;
    private readonly Slider _lowPass;
    private readonly Slider _outputGain;
    private readonly Slider _hiss;
    private VoicePresetDraft? _draft;

    public AtcDeliveryInspector()
    {
        Spacing = 4;
        Margin = new Thickness(8);
        Children.Add(InspectorFields.Header("ATC delivery"));
        Children.Add(_phraseology);
        Children.Add(_radioEffects);
        _drive = InspectorFields.CreateSlider(1.0, 5.0, 2.8, 0.05, v => ApplyIfBound(d => d.Drive = (float)v));
        Children.Add(InspectorFields.Labeled("Drive", _drive));
        _highPass = InspectorFields.CreateSlider(100, 800, 320, 10, v => ApplyIfBound(d => d.HighPassHz = (float)v));
        Children.Add(InspectorFields.Labeled("High-pass Hz", _highPass));
        _lowPass = InspectorFields.CreateSlider(2000, 6000, 3100, 50, v => ApplyIfBound(d => d.LowPassHz = (float)v));
        Children.Add(InspectorFields.Labeled("Low-pass Hz", _lowPass));
        _outputGain = InspectorFields.CreateSlider(0, 12, 5, 0.25, v => ApplyIfBound(d => d.OutputGainDb = (float)v));
        Children.Add(InspectorFields.Labeled("Output gain dB", _outputGain));
        _hiss = InspectorFields.CreateSlider(0, 0.02, 0.004, 0.0005, v => ApplyIfBound(d => d.HissLevel = (float)v));
        Children.Add(InspectorFields.Labeled("Hiss level", _hiss));

        _phraseology.IsCheckedChanged += (_, _) =>
            ApplyIfBound(d => d.UsePhraseology = _phraseology.IsChecked == true);
        _radioEffects.IsCheckedChanged += (_, _) =>
            ApplyIfBound(d => d.ApplyRadioEffects = _radioEffects.IsChecked == true);
    }

    public event EventHandler<VoicePresetDraft>? DraftChanged;

    public void Bind(VoicePresetDraft? draft)
    {
        _draft = draft;
        if (draft is null)
        {
            IsEnabled = false;
            return;
        }

        IsEnabled = true;
        _phraseology.IsChecked = draft.UsePhraseology;
        _radioEffects.IsChecked = draft.ApplyRadioEffects;
        _drive.Value = draft.Drive;
        _highPass.Value = draft.HighPassHz;
        _lowPass.Value = draft.LowPassHz;
        _outputGain.Value = draft.OutputGainDb;
        _hiss.Value = draft.HissLevel;
    }

    private void ApplyIfBound(Action<VoicePresetDraft> apply)
    {
        if (_draft is null)
            return;
        apply(_draft);
        DraftChanged?.Invoke(this, _draft);
    }
}
