using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Novolis.Video.Edit;

namespace Novolis.Avalonia.Video;

/// <summary>
/// Reusable Movie Maker layout: tasks, collections, monitor, storyboard, status.
/// Owns transport, still cache, composer, and preview session for the bound project.
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
        Collections = new MediaCollectionsControl();
        Monitor = new MovieMonitorControl();
        Storyboard = new StoryboardPane();

        Monitor.BindTransport(Transport);
        Storyboard.Bind(Project);
        Storyboard.Strip.SeekRequested += Transport.Seek;
        Storyboard.Strip.ClipSelected += id =>
        {
            _selectedClipId = id;
            Storyboard.SetSelectedClip(id);
        };

        WireTasks();
        Collections.SelectionChanged += RefreshStatus;
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

    /// <summary>Gets the collections pane.</summary>
    public MediaCollectionsControl Collections { get; }

    /// <summary>Gets the preview monitor.</summary>
    public MovieMonitorControl Monitor { get; }

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

    /// <summary>Rebuilds collections/storyboard and refreshes the preview frame.</summary>
    public void Refresh()
    {
        Storyboard.Bind(Project);
        Storyboard.SetSelectedClip(_selectedClipId);
        Collections.Bind(Project);
        _session.Refresh();
        RefreshStatus();
    }

    /// <summary>Opens a file picker and imports stills onto the storyboard.</summary>
    public async Task ImportPicturesAsync(TopLevel topLevel)
    {
        ArgumentNullException.ThrowIfNull(topLevel);
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import pictures",
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
            ImportImagePath(path);
        }

        Refresh();
    }

    /// <summary>Imports one still path into collections + storyboard.</summary>
    public MediaAsset? ImportImagePath(string path, TimeSpan? duration = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var name = Path.GetFileNameWithoutExtension(path);
        var asset = MovieEditOps.AddImage(Project, name, path, duration ?? TimeSpan.FromSeconds(3));
        try
        {
            var frame = AvaloniaStillLoader.LoadBgra(path, Project.Width, Project.Height);
            Stills.SetStill(asset.Id, frame);
        }
        catch (Exception ex)
        {
            _status.Text = $"Import failed for {name}: {ex.Message}";
            return null;
        }

        MovieEditOps.AppendToStoryboard(Project, asset);
        return asset;
    }

    /// <summary>Adds a solid color card and appends it to the storyboard.</summary>
    public MediaAsset AddColorCard(Rgba8? color = null, TimeSpan? duration = null)
    {
        var chosen = color ?? DefaultColors[_colorIndex % DefaultColors.Length];
        _colorIndex++;
        var asset = MovieEditOps.AddColorCard(
            Project,
            $"Color {_colorIndex}",
            chosen,
            duration ?? TimeSpan.FromSeconds(2));
        MovieEditOps.AppendToStoryboard(Project, asset);
        Refresh();
        return asset;
    }

    /// <summary>Appends the selected collections asset to the storyboard.</summary>
    public void AppendSelectedAsset()
    {
        if (Collections.SelectedAssetId is not { } id)
            return;
        var asset = Project.FindAsset(id);
        if (asset is null)
            return;
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
        Refresh();
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
        Tasks.AddToStoryboardRequested += AppendSelectedAsset;
        Tasks.SplitAtPlayheadRequested += SplitAtPlayhead;
        Tasks.RemoveClipRequested += RemoveSelectedClip;
        Tasks.PlayPauseRequested += Transport.Toggle;
        Tasks.RewindRequested += () => Transport.Seek(TimeSpan.Zero);
    }

    void RefreshStatus()
    {
        var dur = StoryboardQuery.TotalDuration(Project);
        _status.Text =
            $"Clips {Project.Clips.Count} · Assets {Project.Assets.Count} · " +
            $"Playhead {Transport.Position:mm\\:ss\\.f} / {dur:mm\\:ss\\.f}" +
            (Transport.IsPlaying ? " · Playing" : " · Paused");
    }

    Control BuildLayout()
    {
        var top = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(new GridLength(220)),
                new ColumnDefinition(new GridLength(280)),
                new ColumnDefinition(GridLength.Star),
            ],
            RowDefinitions = [new RowDefinition(GridLength.Star)],
            Margin = new Thickness(8),
        };
        Grid.SetColumn(Tasks, 0);
        Grid.SetColumn(Collections, 1);
        Grid.SetColumn(Monitor, 2);
        top.Children.Add(Tasks);
        top.Children.Add(Collections);
        top.Children.Add(Monitor);

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
        Collections.SelectionChanged -= RefreshStatus;
        _session.Dispose();
    }
}
