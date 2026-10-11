using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.GraphicalProfile;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using ProfileBinding = Novolis.Avalonia.GraphicalProfile.GraphicalProfileBinding;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Monospace source surface for canonical recipe text or a host-provided DSL.
/// </summary>
public sealed class AssetStudioSourcePanel : AssetStudioPanel
{
    private readonly TextBox _editor;
    private bool _suppressChanges;

    public AssetStudioSourcePanel(string? source = null)
        : base(
            "Source",
            CreateContent(out var editor),
            "assetstudio.panel.source")
    {
        _editor = editor;
        _editor.TextChanged += OnTextChanged;
        SetSource(source ?? string.Empty);
    }

    /// <summary>Editor control used for source editing.</summary>
    public TextBox Editor => _editor;

    /// <summary>Current source text.</summary>
    public string Source
    {
        get => _editor.Text ?? string.Empty;
        set => SetSource(value);
    }

    /// <summary>Raised after user editing changes the source text.</summary>
    public event EventHandler? SourceChanged;

    /// <summary>Replaces source text without raising <see cref="SourceChanged"/>.</summary>
    public void SetSource(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _suppressChanges = true;
        try
        {
            _editor.Text = source;
        }
        finally
        {
            _suppressChanges = false;
        }
    }

    private static Control CreateContent(out TextBox editor)
    {
        editor = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            VerticalContentAlignment = VerticalAlignment.Top,
            MinHeight = 220,
            FontFamily = Profile.MonoFont,
            Padding = new Thickness(8),
        };
        editor.SetValue(
            AutomationProperties.NameProperty,
            "Asset recipe source");
        editor.SetValue(
            AutomationProperties.AutomationIdProperty,
            "assetstudio.source.editor");
        ProfileBinding.Bind(
            editor,
            TextBox.BackgroundProperty,
            Profile.RaisedResourceKey);
        ProfileBinding.Bind(
            editor,
            TextBox.ForegroundProperty,
            Profile.TextResourceKey);
        ProfileBinding.Bind(
            editor,
            TextBox.BorderBrushProperty,
            Profile.BorderResourceKey);
        return editor;
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!_suppressChanges)
        {
            SourceChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
