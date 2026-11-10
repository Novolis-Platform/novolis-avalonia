using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;

namespace Novolis.Avalonia.Speech;

/// <summary>
/// Segmented provider picker bound to a <see cref="SpeechWorkspace"/>.
/// Default selection is Edge; Azure and device voice are optional.
/// </summary>
public sealed class SpeechProviderPicker : Border
{
    public static readonly StyledProperty<SpeechWorkspace?> WorkspaceProperty =
        AvaloniaProperty.Register<SpeechProviderPicker, SpeechWorkspace?>(nameof(Workspace));

    readonly Button _edge;
    readonly Button _azure;
    readonly Button _device;
    SpeechWorkspace? _workspace;

    public SpeechProviderPicker()
    {
        Background = Profile.SurfaceBrush;
        BorderBrush = Profile.BorderBrush;
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(8);
        Padding = new Thickness(8);

        _edge = MakeButton("Edge (default)");
        _azure = MakeButton("Azure");
        _device = MakeButton("Device");
        _edge.Click += async (_, _) =>
        {
            if (_workspace is not null)
                await _workspace.SelectEdgeAsync();
        };
        _azure.Click += async (_, _) =>
        {
            if (_workspace is not null)
                await _workspace.SelectAzureAsync();
        };
        _device.Click += async (_, _) =>
        {
            if (_workspace is not null)
                await _workspace.SelectDeviceAsync();
        };

        Child = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock
                {
                    Text = "Speech provider",
                    FontWeight = FontWeight.SemiBold,
                    Foreground = Profile.TextBrush,
                },
                BuildProviderRow(),
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
            return;

        // Highlight the active provider only. WantAzureSetup opens the setup panel
        // without implying Azure is already the live synthesis path.
        StyleSelected(_edge, _workspace.Provider == SpeechProvider.EdgeTts);
        StyleSelected(_azure, _workspace.Provider == SpeechProvider.AzureSpeech);
        StyleSelected(_device, _workspace.Provider == SpeechProvider.DeviceVoice);
        IsEnabled = !_workspace.IsBusy;
    }

    Grid BuildProviderRow()
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,8,*,8,*"),
        };
        Grid.SetColumn(_edge, 0);
        Grid.SetColumn(_azure, 2);
        Grid.SetColumn(_device, 4);
        row.Children.Add(_edge);
        row.Children.Add(_azure);
        row.Children.Add(_device);
        return row;
    }

    static Button MakeButton(string text) => new()
    {
        Content = text,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        HorizontalContentAlignment = HorizontalAlignment.Center,
        Margin = new Thickness(2),
        Padding = new Thickness(8, 6),
    };

    static void StyleSelected(Button button, bool selected)
    {
        button.Classes.Clear();
        if (selected)
            button.Classes.Add("ngp-action-button");
        button.FontWeight = selected ? FontWeight.SemiBold : FontWeight.Normal;
    }
}
