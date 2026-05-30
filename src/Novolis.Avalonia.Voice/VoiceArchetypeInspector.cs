using Avalonia;
using Avalonia.Controls;
using Novolis.Audio.Voice;
using Novolis.Audio.Voice.Design;

namespace Novolis.Avalonia.Voice;

/// <summary>Edits base archetype fields on a <see cref="VoicePresetDraft"/>.</summary>
public sealed class VoiceArchetypeInspector : StackPanel
{
    private readonly TextBox _profileId = new();
    private readonly TextBox _propertyName = new();
    private readonly ComboBox _modelCombo = new();
    private readonly Slider _speakingRate;
    private readonly Slider _rateMultiplier;
    private readonly TextBox _description = new() { AcceptsReturn = true, MinHeight = 48 };
    private VoicePresetDraft? _draft;

    public VoiceArchetypeInspector()
    {
        Spacing = 4;
        Margin = new Thickness(8);
        Children.Add(InspectorFields.Header("Archetype"));
        Children.Add(InspectorFields.Labeled("Profile id", _profileId));
        Children.Add(InspectorFields.Labeled("C# property name", _propertyName));
        Children.Add(InspectorFields.Labeled("Piper model", _modelCombo));
        _speakingRate = InspectorFields.CreateSlider(0.8, 2.0, 1.24, 0.01, v => ApplyIfBound(d => d.SpeakingRate = (float)v));
        Children.Add(InspectorFields.Labeled("Speaking rate", _speakingRate));
        _rateMultiplier = InspectorFields.CreateSlider(0.8, 1.5, 1.0, 0.01, v => ApplyIfBound(d => d.RateMultiplier = (float)v));
        Children.Add(InspectorFields.Labeled("Preview rate multiplier", _rateMultiplier));
        Children.Add(InspectorFields.Labeled("Description", _description));

        _profileId.TextChanged += (_, _) => ApplyIfBound(d => d.ProfileId = _profileId.Text ?? string.Empty);
        _propertyName.TextChanged += (_, _) => ApplyIfBound(d => d.PropertyName = _propertyName.Text ?? string.Empty);
        _description.TextChanged += (_, _) => ApplyIfBound(d => d.Description = _description.Text ?? string.Empty);
        _modelCombo.SelectionChanged += (_, _) =>
        {
            if (_draft is null || _modelCombo.SelectedItem is not string id)
                return;
            if (VoiceModelCatalog.TryGet(id, out var model))
            {
                _draft.Model = model.Profile;
                DraftChanged?.Invoke(this, _draft);
            }
        };

        foreach (var bundled in VoiceModelCatalog.All)
            _modelCombo.Items.Add(bundled.Profile.Id);
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
        _profileId.Text = draft.ProfileId;
        _propertyName.Text = draft.PropertyName;
        _modelCombo.SelectedItem = draft.Model.Id;
        _speakingRate.Value = draft.SpeakingRate;
        _rateMultiplier.Value = draft.RateMultiplier;
        _description.Text = draft.Description;
    }

    private void ApplyIfBound(Action<VoicePresetDraft> apply)
    {
        if (_draft is null)
            return;
        apply(_draft);
        DraftChanged?.Invoke(this, _draft);
    }
}
