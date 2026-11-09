using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;

namespace Novolis.Avalonia.Speech;

/// <summary>Voice dropdown bound to <see cref="SpeechWorkspace.Voices"/>.</summary>
public sealed class SpeechVoicePicker : Border
{
    public static readonly StyledProperty<SpeechWorkspace?> WorkspaceProperty =
        AvaloniaProperty.Register<SpeechVoicePicker, SpeechWorkspace?>(nameof(Workspace));

    readonly ComboBox _combo = new()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch,
        MinHeight = 36,
    };
    SpeechWorkspace? _workspace;
    bool _suppress;

    public SpeechVoicePicker()
    {
        Background = Profile.SurfaceBrush;
        BorderBrush = Profile.BorderBrush;
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(8);
        Padding = new Thickness(8);
        _combo.SelectionChanged += async (_, _) =>
        {
            if (_suppress || _workspace is null || _combo.SelectedItem is not SpeechVoiceChoice choice)
                return;
            await _workspace.SetVoiceAsync(choice.Id);
        };

        Child = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                new TextBlock
                {
                    Text = "Voice",
                    FontWeight = FontWeight.SemiBold,
                    Foreground = Profile.TextBrush,
                },
                _combo,
            },
        };
    }

    /// <summary>Shared speech session.</summary>
    public SpeechWorkspace? Workspace
    {
        get => GetValue(WorkspaceProperty);
        set => SetValue(WorkspaceProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != WorkspaceProperty)
            return;

        if (_workspace is not null)
            _workspace.Changed -= OnWorkspaceChanged;
        _workspace = Workspace;
        if (_workspace is not null)
            _workspace.Changed += OnWorkspaceChanged;
        Refresh();
    }

    void OnWorkspaceChanged(object? sender, EventArgs e) => Refresh();

    void Refresh()
    {
        if (_workspace is null)
            return;

        _suppress = true;
        try
        {
            _combo.ItemsSource = _workspace.Voices;
            var current = _workspace.Voices.FirstOrDefault(v =>
                string.Equals(v.Id, _workspace.Voice.Voice, StringComparison.OrdinalIgnoreCase));
            _combo.SelectedItem = current ?? _workspace.Voices.FirstOrDefault();
            _combo.IsEnabled = !_workspace.IsBusy && _workspace.Voices.Count > 0;
        }
        finally
        {
            _suppress = false;
        }
    }
}
