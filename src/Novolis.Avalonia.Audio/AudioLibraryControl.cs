using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Audio.Edit;

namespace Novolis.Avalonia.Audio;

/// <summary>Sound library with mini waveforms (Music Maker media pool).</summary>
public sealed class AudioLibraryControl : AudioEditPane
{
    readonly StackPanel _list = new() { Spacing = 6 };
    readonly WaveformControl _previewWave = new() { Height = 56 };
    readonly TextBlock _previewMeta = new() { Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap };
    MusicProject? _project;
    Guid? _selectedId;

    /// <summary>Creates an empty library pane.</summary>
    public AudioLibraryControl()
    {
        Title = "Sound library";
        Body = new DockPanel
        {
            LastChildFill = true,
            Children =
            {
                new Border
                {
                    [DockPanel.DockProperty] = Dock.Bottom,
                    Margin = new Thickness(0, 8, 0, 0),
                    Child = new StackPanel
                    {
                        Spacing = 6,
                        Children =
                        {
                            new Border { Background = Brushes.Black, Child = _previewWave },
                            _previewMeta,
                            MakeButton("Add to selected track", () =>
                            {
                                if (_selectedId is { } id)
                                    AddToTrackRequested?.Invoke(id);
                            }),
                        },
                    },
                },
                new ScrollViewer
                {
                    VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                    Content = _list,
                },
            },
        };
        _previewMeta.Text = "Select a sound, then add it to the arrangement.";
    }

    /// <summary>Raised when selection changes.</summary>
    public event Action? SelectionChanged;

    /// <summary>Raised to place the selected sound on a track.</summary>
    public event Action<Guid>? AddToTrackRequested;

    /// <summary>Gets selected asset id.</summary>
    public Guid? SelectedAssetId => _selectedId;

    /// <summary>Rebuilds library cards.</summary>
    public void Bind(MusicProject project)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _list.Children.Clear();
        foreach (var asset in project.Assets)
            _list.Children.Add(BuildCard(asset));
        UpdatePreview();
    }

    Control BuildCard(SoundAsset asset)
    {
        var wave = new WaveformControl { Height = 36, Width = 200 };
        wave.Bind(asset.Pcm, 80);
        var border = new Border
        {
            Padding = new Thickness(6),
            Background = AudioEditPalette.Pane,
            BorderBrush = _selectedId == asset.Id ? AudioEditPalette.Amber : AudioEditPalette.Border,
            BorderThickness = new Thickness(_selectedId == asset.Id ? 2 : 1),
            Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.Hand),
            Child = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = asset.Name, Foreground = Brushes.White, FontSize = 12 },
                    new TextBlock
                    {
                        Text = $"{asset.Duration.TotalSeconds:0.#}s",
                        Foreground = new SolidColorBrush(Color.FromRgb(160, 180, 190)),
                        FontSize = 10,
                    },
                    new Border { Background = Brushes.Black, Child = wave },
                },
            },
        };
        border.PointerPressed += (_, e) =>
        {
            _selectedId = asset.Id;
            Bind(_project!);
            SelectionChanged?.Invoke();
            if (e.ClickCount >= 2)
                AddToTrackRequested?.Invoke(asset.Id);
        };
        return border;
    }

    void UpdatePreview()
    {
        if (_project is null || _selectedId is not { } id)
        {
            _previewWave.Bind(null);
            return;
        }

        var asset = _project.FindAsset(id);
        if (asset is null)
            return;
        _previewWave.Bind(asset.Pcm, 160);
        _previewMeta.Text = $"{asset.Name}\n{asset.Duration.TotalSeconds:0.#}s · double-click or Add to place on the track";
    }

    static Button MakeButton(string label, Action action)
    {
        var b = new Button
        {
            Content = label,
            Padding = new Thickness(8, 4),
            Background = AudioEditPalette.Accent,
            Foreground = Brushes.White,
        };
        b.Click += (_, _) => action();
        return b;
    }
}
