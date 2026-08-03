using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Novolis.Video.Edit;

namespace Novolis.Avalonia.Video;

/// <summary>
/// Reusable Movie Maker layout: tasks, media library, monitor, transitions, storyboard.
/// </summary>
public sealed class MovieEditWorkspace : Border, IDisposable
{
    readonly TextBlock _title = new()
    {
        FontSize = 22,
        FontFamily = new FontFamily("Segoe UI Semibold"),
        Foreground = Brushes.White,
    };
    readonly TextBlock _status = new() { Foreground = Brushes.White };
    readonly MoviePreviewComposer _composer;
    readonly MoviePreviewSession _session;
    Guid? _selectedClipId;
    int _colorIndex;
    bool _disposed;

    static readonly Rgba8[] DefaultColors =
    [
        new(24, 90, 130),
        new(40, 120, 90),
        new(140, 70, 50),
        new(90, 60, 130),
        new(40, 40, 48),
    ];

    /// <summary>Creates a workspace for <paramref name="project"/>.</summary>
    public MovieEditWorkspace(MovieProject project)
    {
        Project = project ?? throw new ArgumentNullException(nameof(project));
        Transport = new EditTransport();
        Stills = new DecodedStillCache();
        _composer = new MoviePreviewComposer(Stills);

        Tasks = new MovieEditTasksPane();
        Library = new MediaLibraryControl();
        Monitor = new MovieMonitorControl();
        TransitionPanel = new TransitionInspectorControl();
        Storyboard = new StoryboardPane();

        Monitor.BindTransport(Transport);
        Storyboard.Bind(Project);
        Storyboard.Strip.SeekRequested += Transport.Seek;
        Storyboard.Strip.ClipSelected += OnClipSelected;

        WireTasks();
        WireLibrary();
        TransitionPanel.TransitionApplied += () =>
        {
            Storyboard.Bind(Project);
            RefreshStatus();
        };
        Transport.Changed += RefreshStatus;

        _session = new MoviePreviewSession(Project, Transport, _composer, Monitor.Surface, Storyboard.Strip);
        _session.Start();

        Background = MovieEditPalette.Pane;
        _title.Text = Project.Title;
        Child = BuildLayout();
        Refresh();
    }

    /// <summary>Gets the bound project.</summary>
    public MovieProject Project { get; }

    /// <summary>Gets the playhead transport.</summary>
    public EditTransport Transport { get; }

    /// <summary>Gets the still-frame cache used for image preview.</summary>
    public DecodedStillCache Stills { get; }

    /// <summary>Gets the tasks pane.</summary>
    public MovieEditTasksPane Tasks { get; }

    /// <summary>Gets the visual media library.</summary>
    public MediaLibraryControl Library { get; }

    /// <summary>Gets the preview monitor.</summary>
    public MovieMonitorControl Monitor { get; }

    /// <summary>Gets the transition inspector panel.</summary>
    public TransitionInspectorControl TransitionPanel { get; }

    /// <summary>Gets the storyboard pane.</summary>
    public StoryboardPane Storyboard { get; }

    /// <summary>Gets or sets the chrome title.</summary>
    public string HeaderTitle
    {
        get => _title.Text ?? string.Empty;
        set => _title.Text = value;
    }

    /// <summary>Gets the selected storyboard clip id, if any.</summary>
    public Guid? SelectedClipId => _selectedClipId;

    /// <summary>Rebuilds library/storyboard and refreshes the preview frame.</summary>
    public void Refresh()
    {
        Storyboard.Bind(Project);
        Storyboard.SetSelectedClip(_selectedClipId);
        Library.Bind(Project, Stills);
        TransitionPanel.Bind(Project, _selectedClipId);
        _session.Refresh();
        RefreshStatus();
    }

    /// <summary>Opens a file picker and imports stills into the library (not auto-placed).</summary>
    public async Task ImportPicturesAsync(TopLevel topLevel)
    {
        ArgumentNullException.ThrowIfNull(topLevel);
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import pictures into library",
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("Images")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.webp"],
                },
            ],
        });

        foreach (var file in files)
        {
            var path = file.TryGetLocalPath();
            if (path is null)
                continue;
            ImportImagePath(path, appendToStoryboard: false);
        }

        Refresh();
    }

    /// <summary>Imports one still into the library; optionally appends to the storyboard.</summary>
    public MediaAsset? ImportImagePath(string path, TimeSpan? duration = null, bool appendToStoryboard = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var name = Path.GetFileNameWithoutExtension(path);
        var asset = MovieEditOps.AddImage(Project, name, path, duration ?? TimeSpan.FromSeconds(3));
        try
        {
            Stills.SetStill(asset.Id, LoadStill(path, Project.Width, Project.Height));
        }
        catch (Exception ex)
        {
            _status.Text = $"Import failed for {name}: {ex.Message}";
            return null;
        }

        if (appendToStoryboard)
            MovieEditOps.AppendToStoryboard(Project, asset);
        return asset;
    }

    /// <summary>Caches a pre-decoded still for an existing image asset.</summary>
    public void RegisterStill(Guid assetId, Novolis.Video.Rtc.VideoFrame frame) =>
        Stills.SetStill(assetId, frame);

    /// <summary>Exports a playable AVI via folder picker.</summary>
    public async Task ExportAsync(TopLevel topLevel)
    {
        ArgumentNullException.ThrowIfNull(topLevel);
        var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Export movie folder (writes movie.avi)",
            AllowMultiple = false,
        });
        if (folder.Count == 0)
            return;

        var path = folder[0].TryGetLocalPath();
        if (path is null)
        {
            _status.Text = "Export cancelled — no local folder path.";
            return;
        }

        try
        {
            var result = ExportTo(path);
            _status.Text =
                $"Exported video {result.VideoPath} ({result.FrameCount} frames @ {result.FramesPerSecond:0.#} fps)" +
                (result.AudioPath is null ? "" : " + audio");
        }
        catch (Exception ex)
        {
            _status.Text = $"Export failed: {ex.Message}";
        }
    }

    /// <summary>Exports <c>movie.avi</c> into <paramref name="outputDirectory"/>.</summary>
    public MovieExportResult ExportTo(string outputDirectory, double framesPerSecond = 12)
    {
        var exporter = new MovieExporter(new MoviePreviewComposer(Stills));
        return exporter.Export(Project, outputDirectory, framesPerSecond);
    }

    static Novolis.Video.Rtc.VideoFrame LoadStill(string path, int width, int height)
    {
        if (path.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
            return BmpFile.ReadToBgra(path, width, height);
        return AvaloniaStillLoader.LoadBgra(path, width, height);
    }

    /// <summary>Adds a solid color card to the library (not auto-placed).</summary>
    public MediaAsset AddColorCard(Rgba8? color = null, TimeSpan? duration = null, bool appendToStoryboard = false)
    {
        var chosen = color ?? DefaultColors[_colorIndex % DefaultColors.Length];
        _colorIndex++;
        var asset = MovieEditOps.AddColorCard(
            Project,
            $"Color {_colorIndex}",
            chosen,
            duration ?? TimeSpan.FromSeconds(2));
        if (appendToStoryboard)
            MovieEditOps.AppendToStoryboard(Project, asset);
        Refresh();
        return asset;
    }

    /// <summary>Appends a library asset to the storyboard or audio track.</summary>
    public void AddAssetToTimeline(Guid assetId)
    {
        var asset = Project.FindAsset(assetId);
        if (asset is null)
            return;

        if (asset.Kind == MediaKind.Audio)
            MovieEditOps.AppendAudio(Project, asset);
        else
            MovieEditOps.AppendToStoryboard(Project, asset);

        Refresh();
    }

    /// <summary>Splits the clip under the playhead.</summary>
    public void SplitAtPlayhead()
    {
        MovieEditOps.SplitAt(Project, Transport.Position);
        Refresh();
    }

    /// <summary>Removes the selected storyboard clip.</summary>
    public void RemoveSelectedClip()
    {
        if (_selectedClipId is not { } id)
            return;
        MovieEditOps.RemoveClip(Project, id);
        _selectedClipId = null;
        Storyboard.SetSelectedClip(null);
        TransitionPanel.Bind(Project, null);
        Refresh();
    }

    void OnClipSelected(Guid id)
    {
        _selectedClipId = id;
        Storyboard.SetSelectedClip(id);
        TransitionPanel.Bind(Project, id);
        RefreshStatus();
    }

    void WireLibrary()
    {
        Library.SelectionChanged += RefreshStatus;
        Library.AddToStoryboardRequested += id =>
        {
            var asset = Project.FindAsset(id);
            if (asset is null || asset.Kind == MediaKind.Audio)
                return;
            MovieEditOps.AppendToStoryboard(Project, asset);
            Refresh();
        };
        Library.AddToAudioTrackRequested += id =>
        {
            var asset = Project.FindAsset(id);
            if (asset is null || asset.Kind != MediaKind.Audio)
            {
                _status.Text = "Select an audio item in the library to add to the audio track.";
                return;
            }

            MovieEditOps.AppendAudio(Project, asset);
            Refresh();
        };
    }

    void WireTasks()
    {
        Tasks.ImportPicturesRequested += () =>
        {
            var top = TopLevel.GetTopLevel(this);
            if (top is not null)
                _ = ImportPicturesAsync(top);
        };
        Tasks.AddColorCardRequested += () => AddColorCard();
        Tasks.AddToStoryboardRequested += () =>
        {
            if (Library.SelectedAssetId is { } id)
                AddAssetToTimeline(id);
            else
                _status.Text = "Select an item in the media library first.";
        };
        Tasks.SplitAtPlayheadRequested += SplitAtPlayhead;
        Tasks.RemoveClipRequested += RemoveSelectedClip;
        Tasks.ExportRequested += () =>
        {
            var top = TopLevel.GetTopLevel(this);
            if (top is not null)
                _ = ExportAsync(top);
        };
        Tasks.PlayPauseRequested += Transport.Toggle;
        Tasks.RewindRequested += () => Transport.Seek(TimeSpan.Zero);
    }

    void RefreshStatus()
    {
        var dur = StoryboardQuery.TotalDuration(Project);
        var clip = _selectedClipId is { } id ? Project.FindClip(id) : null;
        var transition = clip is null || clip.OutTransition == TransitionKind.None
            ? "none"
            : $"{clip.OutTransition} {clip.OutTransitionDuration.TotalSeconds:0.0}s";
        _status.Text =
            $"Library {Project.Assets.Count} · Storyboard {Project.Clips.Count} · Audio {Project.AudioClips.Count} · " +
            $"Text {Project.TextOverlays.Count} · Transition {transition} · " +
            $"Playhead {Transport.Position:mm\\:ss\\.f} / {dur:mm\\:ss\\.f}" +
            (Transport.IsPlaying ? " · Playing" : " · Paused");
    }

    Control BuildLayout()
    {
        var right = new Grid
        {
            RowDefinitions =
            [
                new RowDefinition(GridLength.Star),
                new RowDefinition(new GridLength(220)),
            ],
        };
        var transitionPanel = TransitionPanel;
        Grid.SetRow(Monitor, 0);
        Grid.SetRow(transitionPanel, 1);
        right.Children.Add(Monitor);
        right.Children.Add(transitionPanel);

        var top = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(new GridLength(200)),
                new ColumnDefinition(new GridLength(360)),
                new ColumnDefinition(GridLength.Star),
            ],
            RowDefinitions = [new RowDefinition(GridLength.Star)],
            Margin = new Thickness(8),
        };
        Grid.SetColumn(Tasks, 0);
        Grid.SetColumn(Library, 1);
        Grid.SetColumn(right, 2);
        top.Children.Add(Tasks);
        top.Children.Add(Library);
        top.Children.Add(right);

        return new DockPanel
        {
            LastChildFill = true,
            Children =
            {
                new Border
                {
                    [DockPanel.DockProperty] = Dock.Top,
                    Background = MovieEditPalette.PaneAlt,
                    Padding = new Thickness(14, 10),
                    Child = _title,
                },
                new Border
                {
                    [DockPanel.DockProperty] = Dock.Bottom,
                    Background = MovieEditPalette.PaneAlt,
                    Padding = new Thickness(12, 6),
                    Child = _status,
                },
                new Border
                {
                    [DockPanel.DockProperty] = Dock.Bottom,
                    Margin = new Thickness(8, 0, 8, 8),
                    Child = Storyboard,
                },
                top,
            },
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Transport.Changed -= RefreshStatus;
        Library.SelectionChanged -= RefreshStatus;
        _session.Dispose();
    }
}
