using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Novolis.Avalonia.Speech;

/// <summary>
/// Composed speech chrome: provider picker, status, voice, and optional Azure setup.
/// Bind one <see cref="SpeechWorkspace"/> and the child controls stay in sync.
/// </summary>
public sealed class SpeechSettingsPanel : StackPanel
{
    public static readonly StyledProperty<SpeechWorkspace?> WorkspaceProperty =
        AvaloniaProperty.Register<SpeechSettingsPanel, SpeechWorkspace?>(nameof(Workspace));

    readonly SpeechProviderPicker _providers = new();
    readonly SpeechStatusCard _status = new();
    readonly SpeechVoicePicker _voices = new();
    readonly AzureSpeechSetupPanel _azure = new();

    public SpeechSettingsPanel()
    {
        Spacing = 10;
        Orientation = Orientation.Vertical;
        Children.Add(_providers);
        Children.Add(_status);
        Children.Add(_voices);
        Children.Add(_azure);
    }

    /// <summary>Shared speech session for all child controls.</summary>
    public SpeechWorkspace? Workspace
    {
        get => GetValue(WorkspaceProperty);
        set => SetValue(WorkspaceProperty, value);
    }

    /// <summary>Provider segmented control.</summary>
    public SpeechProviderPicker Providers => _providers;

    /// <summary>Status card.</summary>
    public SpeechStatusCard Status => _status;

    /// <summary>Voice dropdown.</summary>
    public SpeechVoicePicker Voices => _voices;

    /// <summary>Azure Entra / manual setup panel.</summary>
    public AzureSpeechSetupPanel AzureSetup => _azure;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != WorkspaceProperty)
            return;

        var workspace = Workspace;
        _providers.Workspace = workspace;
        _status.Workspace = workspace;
        _voices.Workspace = workspace;
        _azure.Workspace = workspace;
    }
}
