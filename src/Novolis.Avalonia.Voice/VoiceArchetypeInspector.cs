using Avalonia;
using Avalonia.Controls;
using Novolis.Audio.Voice;
using Novolis.Audio.Voice.Design;
using Novolis.Audio.Voice.Kokoro;
using Novolis.Audio.Voice.Platform;

namespace Novolis.Avalonia.Voice;

/// <summary>Edits base archetype fields on a <see cref="VoicePresetDraft"/>.</summary>
public sealed class VoiceArchetypeInspector : StackPanel
{
    private readonly ComboBox _backendCombo = new();
    private readonly TextBox _profileId = new();
    private readonly TextBox _propertyName = new();
    private readonly ComboBox _modelCombo = new();
    private readonly Slider _speakingRate;
    private readonly Slider _rateMultiplier;
    private readonly TextBox _description = new() { AcceptsReturn = true, MinHeight = 48 };
    private VoicePresetDraft? _draft;
    private bool _suppressEvents;

    public VoiceArchetypeInspector()
    {
        Spacing = 4;
        Margin = new Thickness(8);
        Children.Add(InspectorFields.Header("Archetype"));
        Children.Add(InspectorFields.Labeled("Backend", _backendCombo));
        foreach (VoiceSynthesizerBackend backend in Enum.GetValues<VoiceSynthesizerBackend>())
            _backendCombo.Items.Add(backend);
        _backendCombo.SelectionChanged += (_, _) =>
        {
            if (_suppressEvents || _draft is null || _backendCombo.SelectedItem is not VoiceSynthesizerBackend backend)
                return;
            _draft.Backend = backend;
            if (backend == VoiceSynthesizerBackend.KokoroOnnx
                && !KokoroVoiceCatalog.TryResolveVoiceId(_draft.Model, out _))
            {
                _draft.Model = KokoroVoiceCatalog.All[0].ModelProfile;
            }
            else if (backend == VoiceSynthesizerBackend.SherpaOnnx
                && !VoiceModelCatalog.TryGet(_draft.Model, out _))
            {
                _draft.Model = VoiceModelCatalog.EnUsPiperAmy;
            }

            RebuildModelList();
            DraftChanged?.Invoke(this, _draft);
        };

        Children.Add(InspectorFields.Labeled("Profile id", _profileId));
        Children.Add(InspectorFields.Labeled("C# property name", _propertyName));
        Children.Add(InspectorFields.Labeled("Voice model", _modelCombo));
        _speakingRate = InspectorFields.CreateSlider(1.0, 2.2, 1.4, 0.02, v => ApplyIfBound(d => d.SpeakingRate = (float)v));
        Children.Add(InspectorFields.Labeled("Speaking rate (higher = faster)", _speakingRate));
        _rateMultiplier = InspectorFields.CreateSlider(0.9, 1.4, VoiceEffectChainBuilder.StudioPreviewRateBoost, 0.01, v => ApplyIfBound(d => d.RateMultiplier = (float)v));
        Children.Add(InspectorFields.Labeled("Preview boost", _rateMultiplier));
        Children.Add(InspectorFields.Labeled("Description", _description));

        _profileId.TextChanged += (_, _) => ApplyIfBound(d => d.ProfileId = _profileId.Text ?? string.Empty);
        _propertyName.TextChanged += (_, _) => ApplyIfBound(d => d.PropertyName = _propertyName.Text ?? string.Empty);
        _description.TextChanged += (_, _) => ApplyIfBound(d => d.Description = _description.Text ?? string.Empty);
        _modelCombo.SelectionChanged += (_, _) =>
        {
            if (_suppressEvents || _draft is null || _modelCombo.SelectedItem is not string id)
                return;
            if (_draft.Backend == VoiceSynthesizerBackend.KokoroOnnx)
            {
                if (KokoroVoiceCatalog.All.Any(v => string.Equals(v.VoiceId, id, StringComparison.Ordinal)))
                    _draft.Model = KokoroVoiceCatalog.ToModelProfile(id);
            }
            else if (VoiceModelCatalog.TryGet(id, out var model))
            {
                _draft.Model = model.Profile;
            }

            DraftChanged?.Invoke(this, _draft);
        };
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

        _suppressEvents = true;
        try
        {
            IsEnabled = true;
            _backendCombo.SelectedItem = draft.Backend;
            _profileId.Text = draft.ProfileId;
            _propertyName.Text = draft.PropertyName;
            RebuildModelList();
            _modelCombo.SelectedItem = ResolveModelComboId(draft);
            _speakingRate.Value = draft.SpeakingRate;
            _rateMultiplier.Value = draft.RateMultiplier;
            _description.Text = draft.Description;
            _speakingRate.IsEnabled = draft.Backend != VoiceSynthesizerBackend.Platform;
            _rateMultiplier.IsEnabled = draft.Backend != VoiceSynthesizerBackend.Platform;
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private void RebuildModelList()
    {
        _modelCombo.Items.Clear();
        if (_draft is null)
            return;

        if (_draft.Backend == VoiceSynthesizerBackend.KokoroOnnx)
        {
            foreach (var voice in KokoroVoiceCatalog.All)
                _modelCombo.Items.Add(voice.VoiceId);
            return;
        }

        if (_draft.Backend == VoiceSynthesizerBackend.Platform)
        {
            _modelCombo.Items.Add("(OS default voice)");
            _modelCombo.IsEnabled = false;
            return;
        }

        _modelCombo.IsEnabled = true;
        foreach (var bundled in VoiceModelCatalog.All)
            _modelCombo.Items.Add(bundled.Profile.Id);
    }

    private static string? ResolveModelComboId(VoicePresetDraft draft)
    {
        if (draft.Backend == VoiceSynthesizerBackend.KokoroOnnx
            && KokoroVoiceCatalog.TryResolveVoiceId(draft.Model, out var kokoroId))
            return kokoroId;
        if (draft.Backend == VoiceSynthesizerBackend.Platform)
            return "(OS default voice)";
        return draft.Model.Id;
    }

    private void ApplyIfBound(Action<VoicePresetDraft> apply)
    {
        if (_draft is null || _suppressEvents)
            return;
        apply(_draft);
        DraftChanged?.Invoke(this, _draft);
    }
}
