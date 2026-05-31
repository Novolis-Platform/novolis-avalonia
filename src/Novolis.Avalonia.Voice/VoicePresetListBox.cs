using Avalonia.Controls;
using Novolis.Audio.Voice.Design;
using Novolis.Audio.Voice.Profiles;

namespace Novolis.Avalonia.Voice;

/// <summary>List of editable voice presets seeded from <see cref="VoiceArchetypeCatalog"/>.</summary>
public sealed class VoicePresetListBox : ListBox
{
    private readonly List<VoicePresetDraft> _drafts = [];

    public VoicePresetListBox()
    {
        SelectionChanged += (_, _) =>
        {
            if (SelectedItem is VoicePresetDraft draft)
                SelectionChangedDraft?.Invoke(this, draft);
        };
    }

    public event EventHandler<VoicePresetDraft>? SelectionChangedDraft;

    public IReadOnlyList<VoicePresetDraft> Drafts => _drafts;

    public virtual void LoadCatalogSeeds()
    {
        _drafts.Clear();
        foreach (var archetype in VoiceArchetypeCatalog.All)
            _drafts.Add(VoicePresetDraft.FromArchetype(archetype));

        ItemsSource = _drafts;
        if (_drafts.Count > 0)
            SelectedIndex = 0;
    }

    /// <summary>Appends a preset after <see cref="LoadCatalogSeeds"/> (e.g. dogfood Kokoro variants).</summary>
    public void AddDraft(VoicePresetDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        _drafts.Add(draft);
        ItemsSource = null;
        ItemsSource = _drafts;
    }

    public VoicePresetDraft CloneSelected()
    {
        if (SelectedItem is not VoicePresetDraft source)
            throw new InvalidOperationException("Select a preset to clone.");

        var clone = source.Clone();
        clone.ProfileId = source.ProfileId + "_copy";
        clone.PropertyName = source.PropertyName + "Copy";
        _drafts.Add(clone);
        ItemsSource = null;
        ItemsSource = _drafts;
        SelectedItem = clone;
        return clone;
    }

    public VoicePresetDraft AddBlank()
    {
        var draft = new VoicePresetDraft
        {
            ProfileId = "new_voice",
            PropertyName = "NewVoice",
            Description = "New voice preset",
            RateMultiplier = VoiceEffectChainBuilder.StudioPreviewRateBoost,
        };
        foreach (var step in VoiceEffectChainBuilder.CreateDefaultStudioChain())
            draft.EffectSteps.Add(step.Clone());
        draft.SyncLegacyFlagsFromSteps();
        _drafts.Add(draft);
        ItemsSource = null;
        ItemsSource = _drafts;
        SelectedItem = draft;
        return draft;
    }
}
