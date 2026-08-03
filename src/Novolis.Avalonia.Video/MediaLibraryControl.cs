using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Novolis.Video.Edit;
using Novolis.Video.Rtc;

namespace Novolis.Avalonia.Video;

/// <summary>
/// Visual media library: images / color cards / audio with thumbnail preview and add-to-timeline actions.
/// </summary>
public sealed class MediaLibraryControl : MovieEditPane
{
    readonly WrapPanel _grid = new() { Orientation = Orientation.Horizontal };
    readonly ScrollViewer _scroll;
    readonly Image _previewImage = new() { Stretch = Stretch.Uniform, MaxHeight = 120 };
    readonly TextBlock _previewMeta = new() { Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap };
    readonly StackPanel _previewHost;
    readonly Dictionary<Guid, Bitmap?> _thumbs = [];
    MovieProject? _project;
    DecodedStillCache? _stills;
    Guid? _selectedId;

    /// <summary>Creates an empty media library pane.</summary>
    public MediaLibraryControl()
    {
        Title = "Media library";
        _scroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = _grid,
        };
        _previewHost = new StackPanel
        {
            Spacing = 6,
            Margin = new Thickness(0, 8, 0, 0),
            Children =
            {
                new Border
                {
                    Background = Brushes.Black,
                    Height = 120,
                    Child = _previewImage,
                },
                _previewMeta,
                BuildActionRow(),
            },
        };

        Body = new DockPanel
        {
            LastChildFill = true,
            Children =
            {
                new Border
                {
                    [DockPanel.DockProperty] = Dock.Bottom,
                    Child = _previewHost,
                },
                _scroll,
            },
        };
    }

    /// <summary>Raised when selection changes.</summary>
    public event Action? SelectionChanged;

    /// <summary>Raised when the user wants the selected asset on the storyboard.</summary>
    public event Action<Guid>? AddToStoryboardRequested;

    /// <summary>Raised when the user wants the selected audio on the audio track.</summary>
    public event Action<Guid>? AddToAudioTrackRequested;

    /// <summary>Gets the selected asset id, if any.</summary>
    public Guid? SelectedAssetId => _selectedId;

    /// <summary>Rebuilds cards from project assets and still cache.</summary>
    public void Bind(MovieProject project, DecodedStillCache? stills = null)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _stills = stills;
        RebuildThumbs();
        RebuildCards();
        UpdatePreview();
    }

    StackPanel BuildActionRow()
    {
        var addStory = MakeButton("Add to storyboard", () =>
        {
            if (_selectedId is { } id)
                AddToStoryboardRequested?.Invoke(id);
        });
        var addAudio = MakeButton("Add to audio track", () =>
        {
            if (_selectedId is { } id)
                AddToAudioTrackRequested?.Invoke(id);
        });
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children = { addStory, addAudio },
        };
    }

    void RebuildThumbs()
    {
        foreach (var bmp in _thumbs.Values)
            bmp?.Dispose();
        _thumbs.Clear();
        if (_project is null)
            return;

        foreach (var asset in _project.Assets)
            _thumbs[asset.Id] = MakeThumb(asset);
    }

    Bitmap? MakeThumb(MediaAsset asset)
    {
        try
        {
            if (asset.Kind is MediaKind.Image or MediaKind.Video)
            {
                if (_stills is not null
                    && _stills.TryGetFrame(asset, TimeSpan.Zero, 160, 90, out var cached))
                    return VideoFrameBitmap.ToBitmap(BmpFile.ScaleNearest(cached, 160, 90));

                if (asset.Path is not null
                    && asset.Path.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
                    && File.Exists(asset.Path))
                    return VideoFrameBitmap.ToBitmap(BmpFile.ReadToBgra(asset.Path, 160, 90));
            }

            if (asset.Kind == MediaKind.Color && asset.Color is { } c)
                return VideoFrameBitmap.ToBitmap(SolidColorFrames.Create(160, 90, c));

            if (asset.Kind == MediaKind.Audio)
                return VideoFrameBitmap.ToBitmap(SolidColorFrames.Create(160, 90, new Rgba8(90, 70, 40)));
        }
        catch
        {
            // preview optional
        }

        return null;
    }

    void RebuildCards()
    {
        _grid.Children.Clear();
        if (_project is null)
            return;

        foreach (var asset in _project.Assets)
        {
            var card = BuildCard(asset);
            _grid.Children.Add(card);
        }
    }

    Control BuildCard(MediaAsset asset)
    {
        _thumbs.TryGetValue(asset.Id, out var thumb);
        var image = new Image
        {
            Source = thumb,
            Width = 140,
            Height = 78,
            Stretch = Stretch.UniformToFill,
        };
        var name = new TextBlock
        {
            Text = asset.Name,
            Foreground = Brushes.White,
            FontSize = 11,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 140,
        };
        var meta = new TextBlock
        {
            Text = $"{asset.Kind} · {asset.Duration.TotalSeconds:0.#}s",
            Foreground = new SolidColorBrush(Color.FromRgb(160, 180, 190)),
            FontSize = 10,
        };

        var border = new Border
        {
            Width = 152,
            Margin = new Thickness(0, 0, 8, 8),
            Padding = new Thickness(6),
            Background = MovieEditPalette.Pane,
            BorderBrush = _selectedId == asset.Id ? MovieEditPalette.Amber : MovieEditPalette.Border,
            BorderThickness = new Thickness(_selectedId == asset.Id ? 2 : 1),
            CornerRadius = new CornerRadius(3),
            Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.Hand),
            Child = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new Border { Background = Brushes.Black, Child = image },
                    name,
                    meta,
                },
            },
        };

        border.PointerPressed += (_, e) =>
        {
            _selectedId = asset.Id;
            RebuildCards();
            UpdatePreview();
            SelectionChanged?.Invoke();
            if (e.ClickCount >= 2)
            {
                if (asset.Kind == MediaKind.Audio)
                    AddToAudioTrackRequested?.Invoke(asset.Id);
                else
                    AddToStoryboardRequested?.Invoke(asset.Id);
            }
        };

        return border;
    }

    void UpdatePreview()
    {
        if (_project is null || _selectedId is not { } id)
        {
            _previewImage.Source = null;
            _previewMeta.Text = "Select a library item to preview. Double-click or use Add to place on the timeline.";
            return;
        }

        var asset = _project.FindAsset(id);
        if (asset is null)
            return;

        _thumbs.TryGetValue(id, out var thumb);
        _previewImage.Source = thumb;
        _previewMeta.Text = asset.Kind == MediaKind.Audio
            ? $"{asset.Name}\nAudio · {asset.Duration.TotalSeconds:0.#}s\nAdd to audio track to hear it in export."
            : $"{asset.Name}\n{asset.Kind} · {asset.Duration.TotalSeconds:0.#}s\nAdd to storyboard to place on the timeline.";
    }

    static Button MakeButton(string label, Action action)
    {
        var b = new Button
        {
            Content = label,
            Padding = new Thickness(8, 4),
            Background = MovieEditPalette.Accent,
            Foreground = Brushes.White,
        };
        b.Click += (_, _) => action();
        return b;
    }
}
