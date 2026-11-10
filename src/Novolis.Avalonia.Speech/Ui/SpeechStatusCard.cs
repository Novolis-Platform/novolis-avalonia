using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;

namespace Novolis.Avalonia.Speech;

/// <summary>Compact status card for the current speech provider and capabilities.</summary>
public sealed class SpeechStatusCard : Border
{
    public static readonly StyledProperty<SpeechWorkspace?> WorkspaceProperty =
        AvaloniaProperty.Register<SpeechStatusCard, SpeechWorkspace?>(nameof(Workspace));

    readonly TextBlock _title = new()
    {
        FontSize = 16,
        FontWeight = FontWeight.SemiBold,
        Foreground = Profile.TextBrush,
    };
    readonly TextBlock _detail = new()
    {
        FontSize = 13,
        TextWrapping = TextWrapping.Wrap,
        Foreground = Profile.MutedBrush,
    };
    readonly TextBlock _caps = new()
    {
        FontSize = 12,
        Foreground = Profile.MutedBrush,
    };
    SpeechWorkspace? _workspace;

    public SpeechStatusCard()
    {
        Background = Profile.RaisedBrush;
        BorderBrush = Profile.BorderBrush;
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(8);
        Padding = new Thickness(12);
        Child = new StackPanel
        {
            Spacing = 4,
            Children = { _title, _detail, _caps },
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

    void OnWorkspaceChanged(object? sender, EventArgs e)
    {
        if (global::Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            Refresh();
        else
            global::Avalonia.Threading.Dispatcher.UIThread.Post(Refresh);
    }

    void Refresh()
    {
        if (_workspace is null)
        {
            _title.Text = "Speech";
            _detail.Text = "No workspace bound.";
            _caps.Text = string.Empty;
            return;
        }

        _title.Text = _workspace.StatusTitle;
        _detail.Text = _workspace.StatusDetail;
        var caps = _workspace.Capabilities;
        _caps.Text =
            $"MP3: {(caps.CanCreateMp3 ? "yes" : "no")} · " +
            $"Azure: {(caps.CanAzurePlayback ? "connected" : "off")} · " +
            $"Device: {(caps.CanDevicePlayback ? "yes" : "no")}";
    }
}
