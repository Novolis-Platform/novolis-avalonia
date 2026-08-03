using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Novolis.Audio.Edit;

namespace Novolis.Avalonia.Audio;

/// <summary>
/// Magix Music Maker / Audacity–style workspace: library, multi-track timeline, clip envelope, export.
/// </summary>
public sealed class AudioEditWorkspace : Border, IDisposable
{
    readonly TextBlock _title = new()
    {
        FontSize = 22,
        FontFamily = new FontFamily("Segoe UI Semibold"),
        Foreground = Brushes.White,
    };
    readonly TextBlock _status = new() { Foreground = Brushes.White };
    readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    readonly NaudioPreviewPlayer _player = new();
    readonly AudioTransportBar _transportBar = new();
    Guid? _selectedClipId;
    Guid? _selectedTrackId;
    int _toneIndex;
    bool _disposed;

    static readonly (string Name, double Hz)[] DemoTones =
    [
        ("A3", 220),
        ("C4", 261.63),
        ("E4", 329.63),
        ("G4", 392),
    ];

    /// <summary>Creates a workspace for <paramref name="project"/>.</summary>
    public AudioEditWorkspace(MusicProject project)
    {
        Project = project ?? throw new ArgumentNullException(nameof(project));
        Transport = new AudioTransport();

        Tasks = new AudioEditTasksPane();
        Library = new AudioLibraryControl();
        Timeline = new ArrangementTimelineControl { MinHeight = 180, PixelsPerSecond = 70 };
        ClipPanel = new ClipInspectorControl();

        if (Project.Tracks.Count > 0)
            _selectedTrackId = Project.Tracks[0].Id;

        _transportBar.Bind(Transport);
        Timeline.Bind(Project);
        Timeline.SeekRequested += Transport.Seek;
        Timeline.ClipSelected += OnClipSelected;
        ClipPanel.EnvelopeApplied += Refresh;
        Library.AddToTrackRequested += id => PlaceOnSelectedTrack(id);
        Library.SelectionChanged += RefreshStatus;
        Transport.Changed += OnTransportChanged;
        _timer.Tick += OnTick;

        WireTasks();
        Background = AudioEditPalette.Pane;
        _title.Text = Project.Title;
        Child = BuildLayout();
        Refresh();
        _timer.Start();
    }

    /// <summary>Bound project.</summary>
    public MusicProject Project { get; }

    /// <summary>Transport.</summary>
    public AudioTransport Transport { get; }

    /// <summary>Tasks pane.</summary>
    public AudioEditTasksPane Tasks { get; }

    /// <summary>Sound library.</summary>
    public AudioLibraryControl Library { get; }

    /// <summary>Arrangement timeline.</summary>
    public ArrangementTimelineControl Timeline { get; }

    /// <summary>Clip envelope inspector.</summary>
    public ClipInspectorControl ClipPanel { get; }

    /// <summary>Chrome title.</summary>
    public string HeaderTitle
    {
        get => _title.Text ?? string.Empty;
        set => _title.Text = value;
    }

    /// <summary>Rebuilds UI from the project model.</summary>
    public void Refresh()
    {
        Library.Bind(Project);
        Timeline.Bind(Project);
        Timeline.SetSelectedClip(_selectedClipId);
        Timeline.SetPlayhead(Transport.Position);
        ClipPanel.Bind(Project, _selectedClipId);
        if (_selectedTrackId is null && Project.Tracks.Count > 0)
            _selectedTrackId = Project.Tracks[0].Id;
        RefreshStatus();
    }

    /// <summary>Imports WAV files into the library.</summary>
    public async Task ImportWavAsync(TopLevel topLevel)
    {
        ArgumentNullException.ThrowIfNull(topLevel);
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import WAV into library",
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("WAV") { Patterns = ["*.wav"] },
            ],
        });

        foreach (var file in files)
        {
            var path = file.TryGetLocalPath();
            if (path is null)
                continue;
            try
            {
                AudioEditOps.ImportWav(Project, path);
            }
            catch (Exception ex)
            {
                _status.Text = $"Import failed: {ex.Message}";
            }
        }

        Refresh();
    }

    /// <summary>Exports mix WAV via folder picker.</summary>
    public async Task ExportAsync(TopLevel topLevel)
    {
        ArgumentNullException.ThrowIfNull(topLevel);
        var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Export mix folder (writes mix.wav)",
            AllowMultiple = false,
        });
        if (folder.Count == 0)
            return;
        var path = folder[0].TryGetLocalPath();
        if (path is null)
            return;

        try
        {
            var outPath = Path.Combine(path, "mix.wav");
            ArrangementExporter.ExportWav(Project, outPath);
            _status.Text = $"Exported {outPath}";
        }
        catch (Exception ex)
        {
            _status.Text = $"Export failed: {ex.Message}";
        }
    }

    void WireTasks()
    {
        Tasks.ImportRequested += () =>
        {
            var top = TopLevel.GetTopLevel(this);
            if (top is not null)
                _ = ImportWavAsync(top);
        };
        Tasks.AddToneRequested += () =>
        {
            var tone = DemoTones[_toneIndex % DemoTones.Length];
            _toneIndex++;
            AudioEditOps.AddTone(Project, $"{tone.Name} tone", tone.Hz, TimeSpan.FromSeconds(2));
            Refresh();
        };
        Tasks.AddTrackRequested += () =>
        {
            var track = AudioEditOps.AddTrack(Project, $"Track {Project.Tracks.Count + 1}");
            _selectedTrackId = track.Id;
            Refresh();
        };
        Tasks.AddToTrackRequested += () =>
        {
            if (Library.SelectedAssetId is { } id)
                PlaceOnSelectedTrack(id);
            else
                _status.Text = "Select a sound in the library first.";
        };
        Tasks.SplitRequested += () =>
        {
            if (_selectedClipId is not { } id)
            {
                _status.Text = "Select a clip to split.";
                return;
            }

            AudioEditOps.SplitAt(Project, id, Transport.Position);
            Refresh();
        };
        Tasks.RemoveClipRequested += () =>
        {
            if (_selectedClipId is not { } id)
                return;
            AudioEditOps.RemoveClip(Project, id);
            _selectedClipId = null;
            Refresh();
        };
        Tasks.ExportRequested += () =>
        {
            var top = TopLevel.GetTopLevel(this);
            if (top is not null)
                _ = ExportAsync(top);
        };
        Tasks.PlayPauseRequested += TogglePlayback;
        Tasks.RewindRequested += () =>
        {
            _player.Stop();
            Transport.Seek(TimeSpan.Zero);
        };
    }

    void PlaceOnSelectedTrack(Guid assetId)
    {
        var asset = Project.FindAsset(assetId);
        if (asset is null)
            return;
        var track = _selectedTrackId is { } tid
            ? Project.FindTrack(tid)
            : Project.Tracks.FirstOrDefault();
        if (track is null)
        {
            track = AudioEditOps.AddTrack(Project, "Track 1");
            _selectedTrackId = track.Id;
        }

        var start = ArrangementQuery.TotalDuration(Project);
        // Prefer placing at playhead when on the selected track
        if (Transport.Position < start || start == TimeSpan.Zero)
            start = Transport.Position;

        AudioEditOps.PlaceClip(Project, track, asset, start);
        Refresh();
    }

    void TogglePlayback()
    {
        if (Transport.IsPlaying)
        {
            Transport.Pause();
            _player.Stop();
            return;
        }

        try
        {
            var mix = ArrangementMixer.Render(Project);
            // Seek into mix by slicing from playhead
            var startFrame = (int)(Transport.Position.TotalSeconds * Project.Format.SampleRate);
            if (startFrame >= mix.FrameCount)
            {
                Transport.Seek(TimeSpan.Zero);
                startFrame = 0;
            }

            var bytesPerFrame = mix.Format.BytesPerFrame;
            var sliceBytes = mix.Samples.Slice(startFrame * bytesPerFrame);
            var sliced = new Novolis.Audio.Core.PcmBuffer(
                mix.Format,
                sliceBytes,
                mix.FrameCount - startFrame);
            _player.Play(sliced);
            Transport.Play();
        }
        catch (Exception ex)
        {
            _status.Text = $"Playback failed: {ex.Message}";
        }
    }

    void OnClipSelected(Guid id)
    {
        _selectedClipId = id;
        foreach (var track in Project.Tracks)
        {
            if (track.FindClip(id) is not null)
            {
                _selectedTrackId = track.Id;
                break;
            }
        }

        Timeline.SetSelectedClip(id);
        ClipPanel.Bind(Project, id);
        RefreshStatus();
    }

    void OnTransportChanged()
    {
        Timeline.SetPlayhead(Transport.Position);
        RefreshStatus();
    }

    void OnTick(object? sender, EventArgs e)
    {
        var duration = ArrangementQuery.TotalDuration(Project);
        if (Transport.Tick(TimeSpan.FromMilliseconds(33), duration))
            Timeline.SetPlayhead(Transport.Position);
        if (!Transport.IsPlaying)
            _player.Stop();
    }

    void RefreshStatus()
    {
        var dur = ArrangementQuery.TotalDuration(Project);
        var trackName = _selectedTrackId is { } tid
            ? Project.FindTrack(tid)?.Name ?? "?"
            : "-";
        _status.Text =
            $"Library {Project.Assets.Count} · Tracks {Project.Tracks.Count} · Target {trackName} · " +
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
        var timelineHost = new AudioEditPane
        {
            Title = "Arrangement",
            Body = new DockPanel
            {
                LastChildFill = true,
                Children =
                {
                    new StackPanel
                    {
                        [DockPanel.DockProperty] = Dock.Top,
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        Margin = new Thickness(0, 0, 0, 8),
                        Children = { _transportBar },
                    },
                    new ScrollViewer
                    {
                        HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                        VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                        Content = Timeline,
                    },
                },
            },
        };
        Grid.SetRow(timelineHost, 0);
        Grid.SetRow(ClipPanel, 1);
        right.Children.Add(timelineHost);
        right.Children.Add(ClipPanel);

        var top = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(new GridLength(200)),
                new ColumnDefinition(new GridLength(280)),
                new ColumnDefinition(GridLength.Star),
            ],
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
                    Background = AudioEditPalette.PaneAlt,
                    Padding = new Thickness(14, 10),
                    Child = _title,
                },
                new Border
                {
                    [DockPanel.DockProperty] = Dock.Bottom,
                    Background = AudioEditPalette.PaneAlt,
                    Padding = new Thickness(12, 6),
                    Child = _status,
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
        _timer.Stop();
        _timer.Tick -= OnTick;
        Transport.Changed -= OnTransportChanged;
        Library.SelectionChanged -= RefreshStatus;
        _player.Dispose();
    }
}
