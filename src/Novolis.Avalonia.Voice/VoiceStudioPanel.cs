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
    private readonly VoiceEffectChainInspector _effects = new();
    private readonly VoicePlatformInspector _platform = new();
    private readonly IVoicePresetCodeExport _export;
    private readonly TextBox _phrase = new() { Text = "Tower, ready for departure.", MinWidth = 280 };
    private readonly VoicePreviewController _preview;

    public VoiceStudioPanel(StudioFeedback feedback)
        : this(feedback, new VoicePreviewController(), null)
    {
    }

    public VoiceStudioPanel(StudioFeedback feedback, VoicePreviewController preview)
        : this(feedback, preview, null)
    {
    }

    public VoiceStudioPanel(StudioFeedback feedback, VoicePreviewController preview, IVoicePresetCodeExport? codeExport)
    {
        ArgumentNullException.ThrowIfNull(feedback);
        ArgumentNullException.ThrowIfNull(preview);
        _export = codeExport ?? new VoiceCodeExportPanel();
        _preview = preview;
        ColumnDefinitions = new ColumnDefinitions("220,*");
        RowDefinitions = new RowDefinitions("*");

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

        var playBtn = new Button { Content = "Play", MinWidth = 72 };
        playBtn.Click += async (_, _) => await PlayPreviewAsync(feedback).ConfigureAwait(true);

        var previewBar = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            Margin = new Thickness(8, 8, 8, 0),
        };
        Grid.SetColumn(InspectorFields.Header("Preview"), 0);
        previewBar.Children.Add(InspectorFields.Header("Preview"));
        Grid.SetColumn(_phrase, 1);
        previewBar.Children.Add(_phrase);
        Grid.SetColumn(playBtn, 2);
        previewBar.Children.Add(playBtn);

        var archetypeScroll = new ScrollViewer
        {
            Content = _archetype,
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
        };

        var editorTabs = new TabControl
        {
            Items =
            {
                new TabItem { Header = "Archetype", Content = archetypeScroll },
                new TabItem { Header = "Effect chain", Content = _effects },
                new TabItem { Header = "Platform", Content = _platform },
            },
        };

        var center = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto") };
        Grid.SetRow(previewBar, 0);
        center.Children.Add(previewBar);
        Grid.SetRow(editorTabs, 1);
        center.Children.Add(editorTabs);
        Grid.SetRow(_export.View, 2);
        center.Children.Add(_export.View);

        _archetype.DraftChanged += (_, d) => OnDraftEdited(d, feedback);
        _effects.DraftChanged += (_, d) => OnDraftEdited(d, feedback);
        _platform.DraftChanged += (_, d) => OnDraftEdited(d, feedback);
        _preview.StatusChanged += msg => feedback.SetStatus(msg);

        Grid.SetColumn(left, 0);
        Children.Add(left);
        Grid.SetColumn(center, 1);
        Children.Add(center);

        if (_presets.SelectedItem is VoicePresetDraft initial)
            BindDraft(initial);
    }

    public VoicePreviewController PreviewController => _preview;

    /// <summary>Preset list for host apps to append dogfood seeds after construction.</summary>
    public VoicePresetListBox Presets => _presets;

    private void OnPresetSelected(object? sender, VoicePresetDraft draft) => BindDraft(draft);

    private void BindDraft(VoicePresetDraft draft)
    {
        _archetype.Bind(draft);
        _effects.Bind(draft);
        _platform.Bind(draft);
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
