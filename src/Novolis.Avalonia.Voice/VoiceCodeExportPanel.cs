using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Audio.Voice.Design;

namespace Novolis.Avalonia.Voice;

/// <summary>Read-only C# export with template picker and clipboard copy.</summary>
public sealed class VoiceCodeExportPanel : DockPanel, IVoicePresetCodeExport
{
    Control IVoicePresetCodeExport.View => this;

    private readonly ComboBox _template = new();
    private readonly TextBox _code = new()
    {
        IsReadOnly = true,
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        MinHeight = 160,
        FontFamily = "Consolas,Courier New,monospace",
    };
    private VoicePresetDraft? _draft;

    public VoiceCodeExportPanel()
    {
        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
            Margin = new Thickness(8, 8, 8, 4),
        };
        header.Children.Add(InspectorFields.Header("Generated C#"));
        foreach (VoicePresetCodeTemplate t in Enum.GetValues<VoicePresetCodeTemplate>())
            _template.Items.Add(t);
        _template.SelectedIndex = 0;
        _template.SelectionChanged += (_, _) => RefreshCode();
        Grid.SetColumn(_template, 1);
        header.Children.Add(_template);
        var copy = new Button { Content = "Copy", Margin = new Thickness(8, 0, 0, 0) };
        copy.Click += async (_, _) =>
        {
            if (string.IsNullOrEmpty(_code.Text))
                return;
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is not null)
                await clipboard.SetTextAsync(_code.Text);
        };
        Grid.SetColumn(copy, 2);
        header.Children.Add(copy);

        DockPanel.SetDock(header, Dock.Top);
        Children.Add(header);
        Children.Add(_code);
    }

    public void Bind(VoicePresetDraft? draft)
    {
        _draft = draft;
        RefreshCode();
    }

    private void RefreshCode()
    {
        if (_draft is null || _template.SelectedItem is not VoicePresetCodeTemplate template)
        {
            _code.Text = string.Empty;
            return;
        }

        var validation = VoicePresetValidation.Validate(_draft);
        if (!validation.IsValid)
        {
            _code.Text = "// Fix validation errors:\n// " + string.Join("\n// ", validation.Errors);
            return;
        }

        try
        {
            _code.Text = VoicePresetCodeEmitter.Emit(_draft, template);
        }
        catch (Exception ex)
        {
            _code.Text = $"// {ex.Message}";
        }
    }
}
