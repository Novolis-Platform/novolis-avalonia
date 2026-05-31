using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Novolis.Audio.Voice.Design;

namespace Novolis.Avalonia.Voice;

/// <summary>Dynamic add/remove delivery effect chain for <see cref="VoicePresetDraft"/>.</summary>
public sealed class VoiceEffectChainInspector : Grid
{
    private readonly ListBox _steps = new() { MinWidth = 180 };
    private readonly ComboBox _addKind = new();
    private readonly StackPanel _detail = new() { Spacing = 6 };
    private VoicePresetDraft? _draft;
    private VoiceDeliveryEffectStep? _selectedStep;

    public VoiceEffectChainInspector()
    {
        ColumnDefinitions = new ColumnDefinitions("200,*");
        RowDefinitions = new RowDefinitions("Auto,*,Auto");
        Margin = new Thickness(8);

        var header = InspectorFields.Header("Effect chain (post-TTS)");
        Grid.SetRow(header, 0);
        Grid.SetColumnSpan(header, 2);
        Children.Add(header);

        _steps.SelectionChanged += (_, _) => OnStepSelected();
        var listPanel = new DockPanel();
        DockPanel.SetDock(_steps, Dock.Top);
        listPanel.Children.Add(_steps);

        var listButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 6, 0, 0) };
        var removeBtn = new Button { Content = "Remove" };
        removeBtn.Click += (_, _) => RemoveSelected();
        listButtons.Children.Add(removeBtn);
        DockPanel.SetDock(listButtons, Dock.Bottom);
        listPanel.Children.Add(listButtons);

        Grid.SetRow(listPanel, 1);
        Grid.SetColumn(listPanel, 0);
        Children.Add(listPanel);

        var detailScroll = new ScrollViewer
        {
            Content = _detail,
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
        };
        Grid.SetRow(detailScroll, 1);
        Grid.SetColumn(detailScroll, 1);
        Children.Add(detailScroll);

        foreach (VoiceEffectStepKind kind in Enum.GetValues<VoiceEffectStepKind>())
            _addKind.Items.Add(kind);

        var addRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };
        addRow.Children.Add(new TextBlock { Text = "Add step", VerticalAlignment = VerticalAlignment.Center });
        addRow.Children.Add(_addKind);
        var addBtn = new Button { Content = "Add" };
        addBtn.Click += (_, _) => AddStep();
        addRow.Children.Add(addBtn);
        Grid.SetRow(addRow, 2);
        Grid.SetColumnSpan(addRow, 2);
        Children.Add(addRow);
    }

    public event EventHandler<VoicePresetDraft>? DraftChanged;

    public void Bind(VoicePresetDraft? draft)
    {
        _draft = draft;
        if (draft is null)
        {
            IsEnabled = false;
            _steps.ItemsSource = null;
            _detail.Children.Clear();
            return;
        }

        IsEnabled = true;
        if (draft.EffectSteps.Count == 0)
        {
            foreach (var step in VoiceEffectChainBuilder.CreateDefaultStudioChain())
                draft.EffectSteps.Add(step.Clone());
            draft.SyncLegacyFlagsFromSteps();
        }

        RefreshList();
    }

    private void RefreshList()
    {
        if (_draft is null)
            return;

        _steps.ItemsSource = null;
        _steps.ItemsSource = _draft.EffectSteps;
        _steps.ItemTemplate = new FuncDataTemplate<VoiceDeliveryEffectStep>((step, _) =>
            new TextBlock { Text = step?.DisplayName ?? string.Empty });

        if (_draft.EffectSteps.Count > 0 && _steps.SelectedIndex < 0)
            _steps.SelectedIndex = 0;
        OnStepSelected();
    }

    private void OnStepSelected()
    {
        _detail.Children.Clear();
        if (_steps.SelectedItem is not VoiceDeliveryEffectStep step)
            return;

        _selectedStep = step;
        var enable = new CheckBox { Content = "Enabled", IsChecked = step.Enabled };
        enable.IsCheckedChanged += (_, _) =>
        {
            step.Enabled = enable.IsChecked == true;
            NotifyChanged();
        };
        _detail.Children.Add(enable);

        switch (step.Kind)
        {
            case VoiceEffectStepKind.Phraseology:
                _detail.Children.Add(new TextBlock
                {
                    Text = "Expands digits and ICAO phrasing before synthesis.",
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = 0.8,
                });
                break;
            case VoiceEffectStepKind.BandLimit:
                _detail.Children.Add(InspectorFields.SliderRow("High-pass Hz", step.HighPassHz, 80, 900, 10, v => step.HighPassHz = (float)v, NotifyChanged));
                _detail.Children.Add(InspectorFields.SliderRow("Low-pass Hz", step.LowPassHz, 1800, 8000, 50, v => step.LowPassHz = (float)v, NotifyChanged));
                break;
            case VoiceEffectStepKind.Dynamics:
                _detail.Children.Add(InspectorFields.SliderRow("Drive", step.Drive, 1, 5, 0.05, v => step.Drive = (float)v, NotifyChanged));
                _detail.Children.Add(InspectorFields.SliderRow("Makeup gain", step.MakeupGain, 0.8, 2, 0.05, v => step.MakeupGain = (float)v, NotifyChanged));
                break;
            case VoiceEffectStepKind.OutputGain:
                _detail.Children.Add(InspectorFields.SliderRow("Output gain dB", step.OutputGainDb, -6, 12, 0.25, v => step.OutputGainDb = (float)v, NotifyChanged));
                break;
            case VoiceEffectStepKind.RadioHiss:
                _detail.Children.Add(InspectorFields.SliderRow("Hiss level", step.HissLevel, 0, 0.02, 0.0005, v => step.HissLevel = (float)v, NotifyChanged));
                break;
        }
    }

    private void AddStep()
    {
        if (_draft is null || _addKind.SelectedItem is not VoiceEffectStepKind kind)
            return;

        if (_draft.EffectSteps.Any(s => s.Kind == kind))
        {
            _steps.SelectedItem = _draft.EffectSteps.First(s => s.Kind == kind);
            return;
        }

        _draft.EffectSteps.Add(VoiceDeliveryEffectStep.CreateDefault(kind));
        RefreshList();
        _steps.SelectedItem = _draft.EffectSteps[^1];
        NotifyChanged();
    }

    private void RemoveSelected()
    {
        if (_draft is null || _steps.SelectedItem is not VoiceDeliveryEffectStep step)
            return;

        _draft.EffectSteps.Remove(step);
        RefreshList();
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        if (_draft is null)
            return;
        _draft.SyncLegacyFlagsFromSteps();
        DraftChanged?.Invoke(this, _draft);
    }
}
