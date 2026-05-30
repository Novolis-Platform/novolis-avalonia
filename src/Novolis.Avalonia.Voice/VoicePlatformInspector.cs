using Avalonia;
using Avalonia.Controls;
using Novolis.Audio.Voice.Design;
using Novolis.Audio.Voice.Platform;

namespace Novolis.Avalonia.Voice;

/// <summary>Edits <see cref="PlatformSpeechOptions"/> on a <see cref="VoicePresetDraft"/>.</summary>
public sealed class VoicePlatformInspector : StackPanel
{
    private readonly Slider _pitch;
    private readonly Slider _volume;
    private readonly Slider _rate;
    private readonly TextBox _locale = new();
    private VoicePresetDraft? _draft;

    public VoicePlatformInspector()
    {
        Spacing = 4;
        Margin = new Thickness(8);
        Children.Add(InspectorFields.Header("Platform (OS) speech"));
        Children.Add(new TextBlock
        {
            Text = "OS TTS does not support Novolis radio DSP or WAV export. Use Sherpa or Kokoro for effect-chain preview.",
            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
            Opacity = 0.85,
        });

        _pitch = InspectorFields.CreateSlider(0.5, 2.0, 1.0, 0.05, v => ApplyPlatform(p => p.Pitch = (float)v));
        Children.Add(InspectorFields.Labeled("Pitch", _pitch));
        _volume = InspectorFields.CreateSlider(0.0, 1.0, 1.0, 0.05, v => ApplyPlatform(p => p.Volume = (float)v));
        Children.Add(InspectorFields.Labeled("Volume", _volume));
        _rate = InspectorFields.CreateSlider(0.5, 2.0, 1.0, 0.05, v => ApplyPlatform(p => p.Rate = (float)v));
        Children.Add(InspectorFields.Labeled("Rate", _rate));
        Children.Add(InspectorFields.Labeled("Locale (BCP-47)", _locale));
        _locale.TextChanged += (_, _) => ApplyPlatform(p => p.Locale = string.IsNullOrWhiteSpace(_locale.Text) ? null : _locale.Text);
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

        IsEnabled = draft.Backend == VoiceSynthesizerBackend.Platform;
        var platform = draft.Platform ??= new PlatformSpeechOptions();
        _pitch.Value = platform.Pitch;
        _volume.Value = platform.Volume;
        _rate.Value = platform.Rate;
        _locale.Text = platform.Locale ?? string.Empty;
    }

    private void ApplyPlatform(Action<PlatformSpeechOptions> apply)
    {
        if (_draft is null || _draft.Backend != VoiceSynthesizerBackend.Platform)
            return;
        var platform = _draft.Platform ??= new PlatformSpeechOptions();
        apply(platform);
        DraftChanged?.Invoke(this, _draft);
    }
}
