using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
    readonly ArrangementToolBar _toolBar = new();
    readonly ArrangementEditHistory _history = new();
    readonly Slider _zoom = new()
    {
        Minimum = 30,
        Maximum = 220,
        Value = 70,
        Width = 140,
        VerticalAlignment = VerticalAlignment.Center,
    };
    Guid? _selectedClipId;
    Guid? _selectedTrackId;
    int _toneIndex;
    bool _disposed;
    bool _capturing;
    bool _wasPlaying;

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
        _transportBar.PlayPauseRequested += TogglePlayback;
        _transportBar.RewindRequested += Rewind;
        _toolBar.ToolChanged += tool =>
        {
            Timeline.Tool = tool;
            Timeline.InvalidateVisual();
            RefreshStatus();
        };
        Timeline.Tool = _toolBar.Tool;
        Timeline.Bind(Project);
        Timeline.SeekRequested += OnSeekRequested;
        Timeline.ClipSelected += OnClipSelected;
        Timeline.TrackSelected += OnTrackSelected;
        Timeline.BeforeMutation += CaptureHistory;
        Timeline.ArrangementChanged += Refresh;
        Timeline.SplitAtRequested += OnSplitAt;
        Timeline.DeleteClipRequested += OnDeleteClip;
        Timeline.DrawAtRequested += OnDrawAt;
        ClipPanel.EnvelopeApplying += CaptureHistory;
        ClipPanel.EnvelopeApplied += Refresh;
        Library.AddToTrackRequested += id => PlaceOnSelectedTrack(id);
        Library.SelectionChanged += RefreshStatus;
        Transport.Changed += OnTransportChanged;
        Focusable = true;
        KeyDown += OnKeyDown;
        _zoom.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty)
            {
                Timeline.PixelsPerSecond = _zoom.Value;
                Timeline.InvalidateMeasure();
                Timeline.InvalidateVisual();
            }
        };
        _timer.Tick += OnTick;

        WireTasks();
        Background = AudioEditPalette.Pane;
        _title.Text = Project.Title;
        Child = BuildLayout();
        if (_selectedTrackId is { } tid)
            Timeline.SetSelectedTrack(tid);
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
        Timeline.SetSelectedTrack(_selectedTrackId);
        Timeline.SetPlayhead(Transport.Position);
        Timeline.Tool = _toolBar.Tool;
        ClipPanel.Bind(Project, _selectedClipId);
        if (_selectedTrackId is null && Project.Tracks.Count > 0)
            _selectedTrackId = Project.Tracks[0].Id;
        _transportBar.SetPlaying(Transport.IsPlaying);
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
            CaptureHistory();
            var tone = DemoTones[_toneIndex % DemoTones.Length];
            _toneIndex++;
            AudioEditOps.AddTone(Project, $"{tone.Name} tone", tone.Hz, TimeSpan.FromSeconds(2));
            Refresh();
        };
        Tasks.AddTrackRequested += () =>
        {
            CaptureHistory();
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

            CaptureHistory();
            AudioEditOps.SplitAt(Project, id, Transport.Position);
            Refresh();
        };
        Tasks.DuplicateRequested += () =>
        {
            if (_selectedClipId is not { } id)
            {
                _status.Text = "Select a clip to duplicate.";
                return;
            }

            CaptureHistory();
            var copy = AudioEditOps.DuplicateClip(Project, id);
            if (copy is not null)
                _selectedClipId = copy.Id;
            Refresh();
        };
        Tasks.RemoveClipRequested += () =>
        {
            if (_selectedClipId is not { } id)
                return;
            CaptureHistory();
            AudioEditOps.RemoveClip(Project, id);
            _selectedClipId = null;
            Refresh();
        };
        Tasks.NormalizeRequested += () =>
        {
            var assetId = ResolveSelectedAssetId();
            if (assetId is null)
            {
                _status.Text = "Select a clip or library sound to normalize.";
                return;
            }

            CaptureHistory();
            if (AudioEditOps.NormalizeAsset(Project, assetId.Value))
                _status.Text = "Normalized to −1 dBFS-ish peak.";
            Refresh();
        };
        Tasks.ReverseRequested += () =>
        {
            var assetId = ResolveSelectedAssetId();
            if (assetId is null)
            {
                _status.Text = "Select a clip or library sound to reverse.";
                return;
            }

            CaptureHistory();
            if (AudioEditOps.ReverseAsset(Project, assetId.Value))
                _status.Text = "Reversed asset.";
            Refresh();
        };
        Tasks.UndoRequested += () =>
        {
            if (_history.Undo(Project))
            {
                _selectedClipId = null;
                Refresh();
                _status.Text = "Undo";
            }
        };
        Tasks.RedoRequested += () =>
        {
            if (_history.Redo(Project))
            {
                _selectedClipId = null;
                Refresh();
                _status.Text = "Redo";
            }
        };
        Tasks.ExportRequested += () =>
        {
            var top = TopLevel.GetTopLevel(this);
            if (top is not null)
                _ = ExportAsync(top);
        };
        Tasks.PlayPauseRequested += TogglePlayback;
        Tasks.RewindRequested += Rewind;
    }

    void Rewind()
    {
        _player.Stop();
        Transport.Pause();
        Transport.Seek(TimeSpan.Zero);
        _transportBar.SetPlaying(false);
    }

    void OnSeekRequested(TimeSpan time)
    {
        var playing = Transport.IsPlaying;
        if (playing)
        {
            _player.Stop();
            Transport.Pause();
        }

        Transport.Seek(time);
        if (playing)
            StartPlaybackFromPlayhead();
    }

    void OnTrackSelected(Guid trackId)
    {
        _selectedTrackId = trackId;
        Timeline.SetSelectedTrack(trackId);
        RefreshStatus();
    }

    void OnSplitAt(TimeSpan time)
    {
        CaptureHistory();
        // Prefer the clip under the split time (tool click), not a stale selection.
        Guid id;
        ArrangementClip? under = null;
        foreach (var track in Project.Tracks)
        {
            under = track.Clips.FirstOrDefault(c => c.Contains(time));
            if (under is not null)
                break;
        }

        if (under is not null)
            id = under.Id;
        else if (_selectedClipId is { } selected)
            id = selected;
        else
        {
            _status.Text = "Split: click a clip (Split tool).";
            return;
        }

        var right = AudioEditOps.SplitAt(Project, id, time);
        if (right is not null)
            _selectedClipId = right.Id;
        Refresh();
    }

    void OnDeleteClip(Guid clipId)
    {
        CaptureHistory();
        AudioEditOps.RemoveClip(Project, clipId);
        if (_selectedClipId == clipId)
            _selectedClipId = null;
        Refresh();
    }

    void OnDrawAt(Guid trackId, TimeSpan time)
    {
        if (Library.SelectedAssetId is not { } assetId)
        {
            _status.Text = "Draw: select a library sound first.";
            return;
        }

        var asset = Project.FindAsset(assetId);
        var track = Project.FindTrack(trackId);
        if (asset is null || track is null)
            return;

        CaptureHistory();
        var clip = AudioEditOps.PlaceClip(Project, track, asset, time);
        _selectedTrackId = trackId;
        _selectedClipId = clip.Id;
        Refresh();
    }

    void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            TogglePlayback();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Delete || e.Key == Key.Back)
        {
            if (_selectedClipId is { } id)
            {
                OnDeleteClip(id);
                e.Handled = true;
            }

            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Z)
        {
            if (_history.Undo(Project))
            {
                _selectedClipId = null;
                Refresh();
                _status.Text = "Undo";
            }

            e.Handled = true;
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Y)
        {
            if (_history.Redo(Project))
            {
                _selectedClipId = null;
                Refresh();
                _status.Text = "Redo";
            }

            e.Handled = true;
        }
    }

    Guid? ResolveSelectedAssetId()
    {
        if (_selectedClipId is { } cid && Project.FindClip(cid) is { } clip)
            return clip.AssetId;
        return Library.SelectedAssetId;
    }

    void CaptureHistory()
    {
        if (_capturing)
            return;
        _capturing = true;
        try
        {
            _history.Capture(Project);
        }
        finally
        {
            _capturing = false;
        }
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

        CaptureHistory();
        AudioEditOps.PlaceClip(Project, track, asset, start);
        Refresh();
    }

    void TogglePlayback()
    {
        if (Transport.IsPlaying)
        {
            Transport.Pause();
            _player.Stop();
            _transportBar.SetPlaying(false);
            return;
        }

        StartPlaybackFromPlayhead();
    }

    void StartPlaybackFromPlayhead()
    {
        try
        {
            var mix = ArrangementMixer.Render(Project);
            if (mix.FrameCount <= 1)
            {
                _status.Text = "Nothing to play — add clips to the arrangement.";
                return;
            }

            var duration = ArrangementQuery.TotalDuration(Project);
            var startFrame = (int)(Transport.Position.TotalSeconds * Project.Format.SampleRate);
            if (startFrame >= mix.FrameCount - 1 || Transport.Position >= duration)
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
            _transportBar.SetPlaying(true);
            _status.Text = $"Playing mix ({sliced.Duration:mm\\:ss\\.f} from playhead)…";
        }
        catch (Exception ex)
        {
            Transport.Pause();
            _player.Stop();
            _transportBar.SetPlaying(false);
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
        Timeline.SetSelectedTrack(_selectedTrackId);
        ClipPanel.Bind(Project, id);
        RefreshStatus();
    }

    void OnTransportChanged()
    {
        Timeline.SetPlayhead(Transport.Position);
        _transportBar.SetPlaying(Transport.IsPlaying);
        RefreshStatus();
    }

    void OnTick(object? sender, EventArgs e)
    {
        var duration = ArrangementQuery.TotalDuration(Project);
        if (Transport.Tick(TimeSpan.FromMilliseconds(33), duration))
            Timeline.SetPlayhead(Transport.Position);

        if (_wasPlaying && !Transport.IsPlaying)
            _player.Stop();
        _wasPlaying = Transport.IsPlaying;

        if (Transport.IsPlaying && !_player.IsPlaying)
        {
            Transport.Pause();
            _transportBar.SetPlaying(false);
        }
    }

    void RefreshStatus()
    {
        var dur = ArrangementQuery.TotalDuration(Project);
        var trackName = _selectedTrackId is { } tid
            ? Project.FindTrack(tid)?.Name ?? "?"
            : "-";
        _status.Text =
            $"Tool {_toolBar.Tool} · Target track {trackName} · Library {Project.Assets.Count} · " +
            $"Zoom {_zoom.Value:0} px/s · Playhead {Transport.Position:mm\\:ss\\.f} / {dur:mm\\:ss\\.f}" +
            (Transport.IsPlaying ? " · Playing" : " · Paused") +
            " · Space=play  Del=cut  drag vertical=change track";
    }

    Control BuildLayout()
    {
        var right = new Grid
        {
            RowDefinitions =
            [
                new RowDefinition(GridLength.Star),
                new RowDefinition(new GridLength(200)),
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
                        Spacing = 8,
                        Margin = new Thickness(0, 0, 0, 8),
                        Children =
                        {
                            new StackPanel
                            {
                                Orientation = Orientation.Horizontal,
                                Spacing = 8,
                                Children =
                                {
                                    _transportBar,
                                    new TextBlock
                                    {
                                        Text = "Zoom",
                                        Foreground = Brushes.White,
                                        VerticalAlignment = VerticalAlignment.Center,
                                    },
                                    _zoom,
                                },
                            },
                            _toolBar,
                        },
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
        KeyDown -= OnKeyDown;
        _player.Dispose();
    }
}
