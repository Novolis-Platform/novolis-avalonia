using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Novolis.Audio.Voice.Design;
using Novolis.Avalonia.Studio;

namespace Novolis.Avalonia.Voice;

/// <summary>Composite voice studio: preset list, inspectors, preview, and code export.</summary>
public sealed class VoiceStudioPanel : Grid
{
    private readonly VoicePresetListBox _presets = new();
    private readonly VoiceArchetypeInspector _archetype = new();
    private readonly AtcDeliveryInspector _atc = new();
    private readonly VoiceCodeExportPanel _export = new();
    private readonly TextBox _phrase = new() { Text = "Tower, ready for departure." };
    private readonly VoicePreviewController _preview;

    public VoiceStudioPanel(StudioFeedback feedback)
        : this(feedback, new VoicePreviewController())
    {
    }

    public VoiceStudioPanel(StudioFeedback feedback, VoicePreviewController preview)
    {
        ArgumentNullException.ThrowIfNull(feedback);
        ArgumentNullException.ThrowIfNull(preview);
        _preview = preview;
        RowDefinitions = new RowDefinitions("*,Auto");
        ColumnDefinitions = new ColumnDefinitions("220,*,280");

        _presets.LoadCatalogSeeds();
        _presets.SelectionChangedDraft += OnPresetSelected;

        var presetToolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(8, 8, 8, 0),
        };
        var cloneBtn = new Button { Content = "Clone" };
        cloneBtn.Click += (_, _) =>
        {
            try
            {
                _presets.CloneSelected();
                feedback.Flash("Cloned preset");
            }
            catch (Exception ex)
            {
                feedback.FlashError(ex.Message);
            }
        };
        var newBtn = new Button { Content = "New" };
        newBtn.Click += (_, _) =>
        {
            _presets.AddBlank();
            feedback.Flash("New preset");
        };
        presetToolbar.Children.Add(cloneBtn);
        presetToolbar.Children.Add(newBtn);

        var left = new DockPanel();
        DockPanel.SetDock(presetToolbar, Dock.Top);
        left.Children.Add(presetToolbar);
        var presetHeader = InspectorFields.Header("Presets");
        DockPanel.SetDock(presetHeader, Dock.Top);
        left.Children.Add(presetHeader);
        left.Children.Add(_presets);

        var inspectors = new Grid { RowDefinitions = new RowDefinitions("*,*") };
        Grid.SetRow(_archetype, 0);
        inspectors.Children.Add(_archetype);
        Grid.SetRow(_atc, 1);
        inspectors.Children.Add(_atc);

        var previewBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(8),
        };
        previewBar.Children.Add(InspectorFields.Header("Preview"));
        previewBar.Children.Add(_phrase);
        var playBtn = new Button { Content = "Play" };
        playBtn.Click += async (_, _) => await PlayPreviewAsync(feedback).ConfigureAwait(true);
        previewBar.Children.Add(playBtn);

        var center = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto") };
        Grid.SetRow(previewBar, 0);
        center.Children.Add(previewBar);
        Grid.SetRow(inspectors, 1);
        center.Children.Add(inspectors);
        Grid.SetRow(_export, 2);
        center.Children.Add(_export);

        _archetype.DraftChanged += (_, d) => OnDraftEdited(d, feedback);
        _atc.DraftChanged += (_, d) => OnDraftEdited(d, feedback);

        _preview.StatusChanged += msg => feedback.SetStatus(msg);

        Grid.SetColumn(left, 0);
        Children.Add(left);
        Grid.SetColumn(center, 1);
        Children.Add(center);

        if (_presets.SelectedItem is VoicePresetDraft initial)
            BindDraft(initial);
    }

    public VoicePreviewController PreviewController => _preview;

    private void OnPresetSelected(object? sender, VoicePresetDraft draft) => BindDraft(draft);

    private void BindDraft(VoicePresetDraft draft)
    {
        _archetype.Bind(draft);
        _atc.Bind(draft);
        _export.Bind(draft);
        _preview.PreviewPhrase = _phrase.Text ?? string.Empty;
    }

    private void OnDraftEdited(VoicePresetDraft draft, StudioFeedback feedback)
    {
        _export.Bind(draft);
        feedback.Flash("Preset updated");
        _preview.PreviewPhrase = _phrase.Text ?? string.Empty;
        _preview.SchedulePreview(draft);
    }

    private async Task PlayPreviewAsync(StudioFeedback feedback)
    {
        if (_presets.SelectedItem is not VoicePresetDraft draft)
        {
            feedback.FlashWarning("Select a preset");
            return;
        }

        _preview.PreviewPhrase = _phrase.Text ?? string.Empty;
        await _preview.PreviewNowAsync(draft).ConfigureAwait(true);
    }
}
