using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Novolis.Audio.Core;
using Novolis.Audio.Edit;
using Novolis.Audio.Midi;
using Novolis.Audio.MusicTheory;

namespace Novolis.Avalonia.Audio;

/// <summary>
/// Holistic piano-score studio: transport, multi-track roll, BPM, playhead, ZX keyboard.
/// </summary>
public sealed class MidiPianoWorkspace : Border, IDisposable
{
    readonly TextBlock _title = new()
    {
        FontSize = 20,
        FontFamily = new FontFamily("Segoe UI Semibold"),
        Foreground = Brushes.White,
        Text = "Piano Score",
    };
    readonly TextBlock _hint = new()
    {
        FontSize = 11,
        Foreground = new SolidColorBrush(Color.FromRgb(160, 170, 185)),
        Text = "Space play · R record · [ ] octave · ZXCVBNM+SDGHJ · QWERTYU+23567 upper",
    };
    readonly StackPanel _meters = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
    readonly StackPanel _trackStrip = new() { Orientation = Orientation.Horizontal, Spacing = 6 };
    readonly TextBlock _toast = new()
    {
        FontSize = 12,
        Foreground = AudioEditPalette.Amber,
        TextWrapping = TextWrapping.NoWrap,
        VerticalAlignment = VerticalAlignment.Center,
    };
    readonly NumericUpDown _bpmBox = new()
    {
        Minimum = 40,
        Maximum = 240,
        Increment = 1,
        Width = 72,
        FormatString = "0",
        Background = AudioEditPalette.Pane,
        Foreground = Brushes.White,
        BorderBrush = AudioEditPalette.Border,
    };
    readonly NumericUpDown _meterBox = new()
    {
        Minimum = 1,
        Maximum = 16,
        Increment = 1,
        Width = 56,
        FormatString = "0",
        Background = AudioEditPalette.Pane,
        Foreground = Brushes.White,
        BorderBrush = AudioEditPalette.Border,
    };
    readonly Button _playBtn;
    readonly Button _recordBtn;
    readonly Border _scoreHost;
    readonly DispatcherTimer _playTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    readonly Lazy<MidiPreviewMixer> _mixer = new(() => new MidiPreviewMixer());
    readonly InstrumentBrowserControl _browser = new();
    readonly PianoKeyboardControl _keyboard = new() { LowestMidi = 48, WhiteKeyCount = 21 };
    readonly PianoRollControl _roll = new();
    readonly ScoreStaffControl _staff = new();
    readonly HashSet<Key> _keysDown = [];
    DateTimeOffset _playStarted;
    double _playStartBeat;
    double _playLengthBeats;
    int _octaveOffset;
    bool _scoreVisible = true;
    bool _disposed;
    bool _suppressMeterEvents;
    ChordQuality _paintQuality = ChordQuality.MajorSeventh;

    public MidiPianoWorkspace(MidiPianoSession? session = null, MusicProject? musicProject = null)
    {
        MusicProject = musicProject;
        Session = session ?? new MidiPianoSession(
            format: musicProject?.Format ?? new PcmFormat(44_100, Channels: 1, PcmSampleFormat.Int16));
        Focusable = true;

        _playBtn = MakePrimaryButton("▶  Play", PlayScoreAsync);
        _recordBtn = MakeToolButton("●  Record", ToggleRecordAsync);
        _bpmBox.Value = (decimal)Session.Score.TempoBpm;
        _meterBox.Value = Session.Score.BeatsPerBar;
        _bpmBox.ValueChanged += (_, e) =>
        {
            if (_suppressMeterEvents || e.NewValue is null)
                return;
            Session.SetTempoBpm((double)e.NewValue.Value);
            Toast($"Tempo {Session.Score.TempoBpm:0} BPM");
        };
        _meterBox.ValueChanged += (_, e) =>
        {
            if (_suppressMeterEvents || e.NewValue is null)
                return;
            Session.Score.SetMeter((int)e.NewValue.Value, Session.Score.BeatUnit);
            _roll.InvalidateMeasure();
            _roll.InvalidateVisual();
            RefreshChrome();
            Toast($"Meter {Session.Score.BeatsPerBar}/{Session.Score.BeatUnit}");
        };
        _playTimer.Tick += OnPlayTimerTick;

        _browser.Bind(Session.Bank, Session.SelectedPatch.Id);
        _browser.PatchSelected += patch =>
        {
            Session.SelectPatch(patch);
            RefreshChrome();
        };
        _keyboard.NoteOn += OnKeyboardNoteOn;
        _keyboard.NoteOff += OnKeyboardNoteOff;
        _roll.Bind(Session.Score);
        _staff.Bind(Session.Score);
        _roll.SelectionChanged += id =>
        {
            Session.SelectNote(id);
            _roll.SetSelected(id);
            RefreshChrome();
        };
        _roll.PreviewNote += midi =>
        {
            try { _mixer.Value.Play(MidiSynth.RenderNote(Session.Format, Session.SelectedPatch, midi, TimeSpan.FromMilliseconds(280))); }
            catch { /* ignore */ }
        };
        _roll.ScoreEdited += RefreshChrome;
        Session.Changed += () =>
        {
            _keyboard.SetPressed(Session.HeldMidiNumbers);
            _roll.SetSelected(Session.SelectedNoteId);
            RefreshChrome();
        };

        _scoreHost = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(32, 36, 42)),
            BorderBrush = AudioEditPalette.Border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(4),
            Child = new ScrollViewer
            {
                HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                MaxHeight = 140,
                Content = _staff,
            },
        };

        Background = AudioEditPalette.Pane;
        Child = BuildLayout();
        RefreshChrome();
        AttachedToVisualTree += (_, _) => Focus();
    }

    public MidiPianoSession Session { get; }
    public MusicProject? MusicProject { get; }

    public string HeaderTitle
    {
        get => _title.Text ?? "";
        set => _title.Text = value;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Space)
        {
            _ = PlayScoreAsync();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.R && !e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            _ = ToggleRecordAsync();
            e.Handled = true;
            return;
        }

        if (e.Key is Key.Delete or Key.Back)
        {
            DeleteSelected();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.OemOpenBrackets)
        {
            _octaveOffset = Math.Max(-24, _octaveOffset - 12);
            RefreshChrome();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.OemCloseBrackets)
        {
            _octaveOffset = Math.Min(36, _octaveOffset + 12);
            RefreshChrome();
            e.Handled = true;
            return;
        }

        if (PianoComputerKeyboard.TryMap(e.Key, out _) && _keysDown.Add(e.Key))
        {
            var midi = PianoComputerKeyboard.ToMidi(e.Key, _octaveOffset);
            if (midi >= 0)
                OnKeyboardNoteOn(midi);
            e.Handled = true;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (!_keysDown.Remove(e.Key))
            return;
        var midi = PianoComputerKeyboard.ToMidi(e.Key, _octaveOffset);
        if (midi >= 0)
            OnKeyboardNoteOff(midi);
        e.Handled = true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _playTimer.Stop();
        Session.AllNotesOff();
        if (_mixer.IsValueCreated)
            _mixer.Value.Dispose();
    }

    void OnKeyboardNoteOn(int midi)
    {
        try
        {
            var pcm = Session.NoteOn(midi);
            _mixer.Value.Play(pcm);
            _keyboard.SetPressed(Session.HeldMidiNumbers);
            RefreshChrome();
        }
        catch (Exception ex)
        {
            Toast(ex.Message);
        }
    }

    void OnKeyboardNoteOff(int midi)
    {
        Session.NoteOff(midi);
        _keyboard.SetPressed(Session.HeldMidiNumbers);
        RefreshChrome();
    }

    Control BuildLayout()
    {
        var transport = BuildTransportBar();
        var writeRow = BuildWriteBar();
        var fileRow = BuildFileBar();

        var rollScroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = _roll,
        };

        var stage = new DockPanel { LastChildFill = true };
        stage.Children.Add(new Border
        {
            [DockPanel.DockProperty] = Dock.Top,
            Margin = new Thickness(0, 0, 0, 8),
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    SectionLabel("Score preview", "Toggle from transport · matches PDF export"),
                    _scoreHost,
                },
            },
        });
        stage.Children.Add(new DockPanel
        {
            LastChildFill = true,
            Children =
            {
                SectionLabel("Piano roll", "Drag · resize · Ctrl+chord")
                    .With(c => { DockPanel.SetDock(c, Dock.Top); }),
                new Border
                {
                    Background = AudioEditPalette.PaneAlt,
                    BorderBrush = AudioEditPalette.Border,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Child = rollScroll,
                    Margin = new Thickness(0, 6, 0, 0),
                },
            },
        });

        var body = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("220,*"),
            ColumnSpacing = 12,
            Margin = new Thickness(0, 10, 0, 0),
        };
        _browser.MinWidth = 200;
        var rail = new Border
        {
            Background = AudioEditPalette.PaneAlt,
            BorderBrush = AudioEditPalette.Border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(4),
            Child = _browser,
        };
        Grid.SetColumn(rail, 0);
        Grid.SetColumn(stage, 1);
        body.Children.Add(rail);
        body.Children.Add(stage);

        var performer = new Border
        {
            Background = AudioEditPalette.PaneAlt,
            BorderBrush = AudioEditPalette.Border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10, 8),
            Margin = new Thickness(0, 10, 0, 0),
            Child = new DockPanel
            {
                LastChildFill = true,
                Children =
                {
                    new StackPanel
                    {
                        [DockPanel.DockProperty] = Dock.Top,
                        Orientation = Orientation.Horizontal,
                        Spacing = 12,
                        Margin = new Thickness(0, 0, 0, 6),
                        Children =
                        {
                            SectionLabel("Perform", "ZXCVBNM + SDGHJ · QWERTYU upper · [ ] octave"),
                            _toast,
                        },
                    },
                    new ScrollViewer
                    {
                        HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                        VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                        Content = _keyboard,
                    },
                },
            },
        };

        return new DockPanel
        {
            Margin = new Thickness(14),
            LastChildFill = true,
            Children =
            {
                new StackPanel
                {
                    [DockPanel.DockProperty] = Dock.Top,
                    Spacing = 8,
                    Children =
                    {
                        BuildHeader(),
                        transport,
                        BuildTrackBar(),
                        writeRow,
                        fileRow,
                    },
                },
                performer.With(c => DockPanel.SetDock(c, Dock.Bottom)),
                body,
            },
        };
    }

    Control BuildHeader()
    {
        return new DockPanel
        {
            LastChildFill = true,
            Children =
            {
                new StackPanel
                {
                    Spacing = 2,
                    Children = { _title, _hint },
                },
                new StackPanel
                {
                    [DockPanel.DockProperty] = Dock.Right,
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children = { _meters },
                },
            },
        };
    }

    Control BuildTransportBar()
    {
        var stop = MakeToolButton("■  Stop", async () =>
        {
            StopPlayback();
            Session.StopRecording();
            Session.AllNotesOff();
            await Task.CompletedTask;
        });

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                GroupLabel("TRANSPORT"),
                _recordBtn,
                _playBtn,
                stop,
                VSep(),
                GroupLabel("BPM"),
                _bpmBox,
                GroupLabel("Meter"),
                _meterBox,
                new TextBlock
                {
                    Text = "/4",
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                },
                VSep(),
                MakeToolButton("Zoom +", async () => { _roll.Zoom(1.25, 1.1); await Task.CompletedTask; }),
                MakeToolButton("Zoom −", async () => { _roll.Zoom(0.8, 0.9); await Task.CompletedTask; }),
                MakeToolButton("Score on/off", async () =>
                {
                    _scoreVisible = !_scoreVisible;
                    _scoreHost.IsVisible = _scoreVisible;
                    await Task.CompletedTask;
                }),
            },
        };

        return ToolStrip(row, accent: true);
    }

    Control BuildWriteBar()
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                GroupLabel("WRITE"),
                MakeToolButton("Paint maj7", () => SetPaint(ChordQuality.MajorSeventh, "maj7")),
                MakeToolButton("Paint m7", () => SetPaint(ChordQuality.MinorSeventh, "m7")),
                MakeToolButton("Paint dom7", () => SetPaint(ChordQuality.DominantSeventh, "dom7")),
                VSep(),
                MakeToolButton("+4 bars", async () =>
                {
                    Session.Score.GrowBars(4);
                    _roll.InvalidateMeasure();
                    _roll.InvalidateVisual();
                    RefreshChrome();
                    await Task.CompletedTask;
                }),
                MakeToolButton("Delete", async () => { DeleteSelected(); await Task.CompletedTask; }),
                MakeToolButton("Clear", async () =>
                {
                    Session.Score.Clear();
                    Session.SelectNote(null);
                    RefreshChrome();
                    Toast("Score cleared.");
                    await Task.CompletedTask;
                }),
            },
        };
        return ToolStrip(row);
    }

    Control BuildFileBar()
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                GroupLabel("FILE"),
                MakeToolButton("Save MIDI…", SaveMidiAsync),
                MakeToolButton("Load MIDI…", LoadMidiAsync),
                MakePrimaryButton("Export PDF…", ExportPdfAsync, compact: true),
                VSep(),
                MakeToolButton("Save patch…", SavePatchAsync),
                MakeToolButton("Load patch…", LoadPatchAsync),
                MakeToolButton("Save bank…", SaveBankAsync),
                MakeToolButton("Import bank…", ImportBankAsync),
                MakeToolButton("Bounce to Arrangement", BounceToLibraryAsync),
            },
        };
        return ToolStrip(row);
    }

    Task SetPaint(ChordQuality quality, string label)
    {
        _paintQuality = quality;
        _roll.ChordPaintQuality = quality;
        Toast($"Ctrl+click paints {label} voicing.");
        return Task.CompletedTask;
    }

    async Task ToggleRecordAsync()
    {
        if (Session.IsRecording)
        {
            Session.StopRecording();
            Toast("Recording stopped.");
        }
        else
        {
            Session.StartRecording(clearExisting: false);
            Toast("Recording — play keys; notes append to the score.");
        }

        RefreshChrome();
        await Task.CompletedTask;
    }

    Control BuildTrackBar()
    {
        RebuildTrackStrip();
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                GroupLabel("PARTS"),
                _trackStrip,
                MakeToolButton("+ Part", async () =>
                {
                    var idx = Session.Score.Tracks.Count;
                    var colors = ScoreTrackColors.Palette;
                    var track = Session.Score.AddTrack(new ScoreTrack(
                        $"Part {idx + 1}",
                        Session.SelectedPatch.Id,
                        colorIndex: idx % colors.Length));
                    Session.SelectTrack(track.Id);
                    Toast($"Added {track.Name}");
                    await Task.CompletedTask;
                }),
            },
        };
        return ToolStrip(row);
    }

    void RebuildTrackStrip()
    {
        _trackStrip.Children.Clear();
        foreach (var track in Session.Score.Tracks)
        {
            var (r, g, b) = ScoreTrackColors.Rgb(track.ColorIndex);
            var active = track.Id == Session.Score.ActiveTrackId;
            var btn = new Button
            {
                Content = track.Mute ? $"{track.Name} (mute)" : track.Name,
                Padding = new Thickness(10, 5),
                Background = new SolidColorBrush(Color.FromRgb(r, g, b)),
                Foreground = Brushes.White,
                BorderBrush = active ? Brushes.White : AudioEditPalette.Border,
                BorderThickness = new Thickness(active ? 2 : 1),
                FontFamily = new FontFamily("Segoe UI Semibold"),
                FontSize = 12,
            };
            var captured = track;
            btn.Click += (_, _) =>
            {
                Session.SelectTrack(captured.Id);
                Toast($"Writing on {captured.Name} · {captured.PatchId}");
                RefreshChrome();
            };
            btn.DoubleTapped += (_, _) =>
            {
                captured.Mute = !captured.Mute;
                Session.Score.NotifyChanged();
                RefreshChrome();
            };
            _trackStrip.Children.Add(btn);
        }
    }

    async Task PlayScoreAsync()
    {
        Session.StopRecording();
        if (Session.Score.Notes.Count == 0)
        {
            Toast("Score is empty.");
            return;
        }

        try
        {
            StopPlayback(resetHead: false);
            _playStartBeat = 0;
            _playLengthBeats = Math.Max(Session.Score.TotalBeats, Session.Score.ContentEndBeat);
            _playStarted = DateTimeOffset.UtcNow;
            Session.SetPlayhead(0, playing: true);
            _roll.SetPlayhead(0);
            _mixer.Value.Play(Session.RenderSequence());
            _playTimer.Start();
            Toast($"Playing “{Session.Score.Title}”…");
        }
        catch (Exception ex)
        {
            Toast(ex.Message);
        }

        RefreshChrome();
        await Task.CompletedTask;
    }

    void OnPlayTimerTick(object? sender, EventArgs e)
    {
        if (!Session.IsPlaying)
        {
            _playTimer.Stop();
            return;
        }

        var elapsedMin = (DateTimeOffset.UtcNow - _playStarted).TotalMinutes;
        var beat = _playStartBeat + elapsedMin * Session.Score.TempoBpm;
        if (beat >= _playLengthBeats)
        {
            StopPlayback(resetHead: true);
            Toast("Playback finished.");
            return;
        }

        Session.SetPlayhead(beat, playing: true);
        _roll.SetPlayhead(beat);
    }

    void StopPlayback(bool resetHead = true)
    {
        _playTimer.Stop();
        _mixer.Value.Stop();
        if (resetHead)
        {
            Session.SetPlayhead(0, playing: false);
            _roll.SetPlayhead(null);
        }
        else
        {
            Session.StopPlaybackUi();
            _roll.SetPlayhead(Session.PlayheadBeat);
        }

        RefreshChrome();
    }

    void DeleteSelected()
    {
        if (Session.SelectedNoteId is { } id)
            Session.Score.Remove(id);
        Session.SelectNote(null);
        _roll.SetSelected(null);
        RefreshChrome();
    }

    async Task ExportPdfAsync()
    {
        var path = await PickSaveAsync("Full score PDF", "pdf", suggested: "score.pdf");
        if (path is null)
            return;
        Session.ExportPdf(path);
        Toast($"PDF → {path}");
    }

    async Task SaveMidiAsync()
    {
        var path = await PickSaveAsync("MIDI sequence", "mid", suggested: "score.mid");
        if (path is null)
            return;
        Session.Score.Title = Path.GetFileNameWithoutExtension(path);
        Session.SaveMidi(path);
        Toast($"MIDI → {path}");
    }

    async Task LoadMidiAsync()
    {
        var path = await PickOpenAsync("MIDI sequence", "mid");
        if (path is null)
            return;
        Session.LoadMidi(path);
        _browser.Bind(Session.Bank, Session.SelectedPatch.Id);
        _roll.InvalidateMeasure();
        _roll.InvalidateVisual();
        Toast($"Loaded {Session.Score.Notes.Count} notes.");
        RefreshChrome();
    }

    async Task SavePatchAsync()
    {
        var path = await PickSaveAsync("Instrument patch", "json");
        if (path is null)
            return;
        Session.SaveSelectedPatch(path);
        Toast($"Patch → {path}");
    }

    async Task LoadPatchAsync()
    {
        var path = await PickOpenAsync("Instrument patch", "json");
        if (path is null)
            return;
        Session.LoadPatchIntoBank(path);
        _browser.Bind(Session.Bank, Session.SelectedPatch.Id);
        Toast($"Patch loaded · {Session.SelectedPatch.Name}");
        RefreshChrome();
    }

    async Task SaveBankAsync()
    {
        var path = await PickSaveAsync("Instrument bank", "json");
        if (path is null)
            return;
        Session.SaveBank(path);
        Toast($"Bank ({Session.Bank.Patches.Count}) → {path}");
    }

    async Task ImportBankAsync()
    {
        var path = await PickOpenAsync("Instrument bank", "json");
        if (path is null)
            return;
        Session.ImportBank(path);
        _browser.Bind(Session.Bank, Session.SelectedPatch.Id);
        Toast($"Bank now {Session.Bank.Patches.Count} sounds.");
        RefreshChrome();
    }

    async Task BounceToLibraryAsync()
    {
        if (MusicProject is null)
        {
            Toast("No Arrangement project attached.");
            return;
        }

        if (Session.Score.Notes.Count == 0)
        {
            var demo = new MidiSequence("Patch Preview");
            demo.Add(new MidiNoteEvent(60, 100, TimeSpan.Zero, TimeSpan.FromMilliseconds(400)));
            demo.Add(new MidiNoteEvent(64, 100, TimeSpan.FromMilliseconds(80), TimeSpan.FromMilliseconds(400)));
            demo.Add(new MidiNoteEvent(67, 100, TimeSpan.FromMilliseconds(160), TimeSpan.FromMilliseconds(400)));
            AudioEditOps.AddPcm(MusicProject, $"{Session.SelectedPatch.Name} preview",
                MidiSynth.RenderSequence(Session.Format, Session.SelectedPatch, demo));
            Toast($"Bounced {Session.SelectedPatch.Name} preview → Arrangement.");
            return;
        }

        AudioEditOps.AddPcm(MusicProject, Session.Score.Title, Session.RenderSequence());
        Toast($"Bounced “{Session.Score.Title}” → Arrangement library.");
        await Task.CompletedTask;
    }

    async Task<string?> PickSaveAsync(string title, string ext, string? suggested = null)
    {
        if (TopLevel.GetTopLevel(this) is not { StorageProvider: { } sp })
            return null;
        var file = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            DefaultExtension = ext,
            SuggestedFileName = suggested ?? (ext == "mid" ? "score.mid" : ext == "pdf" ? "score.pdf" : "sounds.json"),
            FileTypeChoices = [new FilePickerFileType(title) { Patterns = [$"*.{ext}"] }],
        });
        return file?.TryGetLocalPath();
    }

    async Task<string?> PickOpenAsync(string title, string ext)
    {
        if (TopLevel.GetTopLevel(this) is not { StorageProvider: { } sp })
            return null;
        var files = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(title) { Patterns = [$"*.{ext}"] }],
        });
        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    void RefreshChrome()
    {
        _recordBtn.Content = Session.IsRecording ? "■  Stop rec" : "●  Record";
        _recordBtn.Background = Session.IsRecording
            ? new SolidColorBrush(Color.FromRgb(160, 55, 55))
            : AudioEditPalette.PaneAlt;
        _roll.ChordPaintQuality = _paintQuality;
        RebuildTrackStrip();

        _suppressMeterEvents = true;
        _bpmBox.Value = (decimal)Session.Score.TempoBpm;
        _meterBox.Value = Session.Score.BeatsPerBar;
        _suppressMeterEvents = false;

        _meters.Children.Clear();
        _meters.Children.Add(MeterChip(
            Session.IsPlaying ? "PLAY" : Session.IsRecording ? "REC" : "READY",
            Session.IsPlaying ? Color.FromRgb(50, 160, 140)
                : Session.IsRecording ? Color.FromRgb(200, 70, 70)
                : Color.FromRgb(55, 65, 78)));
        _meters.Children.Add(MeterChip($"{Session.Score.Notes.Count} notes"));
        _meters.Children.Add(MeterChip($"{Session.Score.BarCount} bars"));
        _meters.Children.Add(MeterChip($"{Session.Score.TempoBpm:0} BPM"));
        _meters.Children.Add(MeterChip($"{Session.Score.BeatsPerBar}/{Session.Score.BeatUnit}"));
        var active = Session.Score.ActiveTrack;
        if (active is not null)
        {
            var (r, g, b) = ScoreTrackColors.Rgb(active.ColorIndex);
            _meters.Children.Add(MeterChip(active.Name, Color.FromRgb(r, g, b)));
        }

        var oct = 3 + _octaveOffset / 12;
        _meters.Children.Add(MeterChip($"Z=C{oct}"));
        if (Session.IsPlaying)
            _meters.Children.Add(MeterChip($"▶ {Session.PlayheadBeat:0.0}"));
        if (Session.SelectedNoteId is { } id && Session.Score.Find(id) is { } n)
            _meters.Children.Add(MeterChip($"{ScoreNotation.Name(n.MidiNumber)} @ {n.StartBeat:0.##}"));
    }

    void Toast(string message) => _toast.Text = message;

    static Border ToolStrip(Control content, bool accent = false) =>
        new()
        {
            Background = accent
                ? new SolidColorBrush(Color.FromRgb(30, 42, 48))
                : AudioEditPalette.PaneAlt,
            BorderBrush = accent ? AudioEditPalette.Accent : AudioEditPalette.Border,
            BorderThickness = new Thickness(accent ? 1.5 : 1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 8),
            Child = new ScrollViewer
            {
                HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                Content = content,
            },
        };

    static TextBlock GroupLabel(string text) =>
        new()
        {
            Text = text,
            FontSize = 10,
            FontFamily = new FontFamily("Segoe UI Semibold"),
            Foreground = new SolidColorBrush(Color.FromRgb(140, 155, 170)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0),
            MinWidth = 72,
        };

    static Control SectionLabel(string title, string subtitle)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = title,
                    FontSize = 13,
                    FontFamily = new FontFamily("Segoe UI Semibold"),
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                },
                new TextBlock
                {
                    Text = subtitle,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 165)),
                    VerticalAlignment = VerticalAlignment.Center,
                },
            },
        };
    }

    static Border MeterChip(string text, Color? accent = null)
    {
        var color = accent ?? Color.FromRgb(55, 65, 78);
        return new Border
        {
            Background = new SolidColorBrush(color),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10, 4),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI Semibold"),
                Foreground = Brushes.White,
            },
        };
    }

    static Border VSep() =>
        new()
        {
            Width = 1,
            Height = 22,
            Background = AudioEditPalette.Border,
            Margin = new Thickness(4, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };

    Button MakePrimaryButton(string label, Func<Task> action, bool compact = false)
    {
        var btn = new Button
        {
            Content = label,
            Padding = compact ? new Thickness(12, 6) : new Thickness(16, 8),
            Background = AudioEditPalette.Accent,
            Foreground = Brushes.White,
            BorderBrush = AudioEditPalette.Accent,
            FontFamily = new FontFamily("Segoe UI Semibold"),
            FontSize = compact ? 12 : 13,
        };
        Wire(btn, action);
        return btn;
    }

    Button MakeToolButton(string label, Func<Task> action)
    {
        var btn = new Button
        {
            Content = label,
            Padding = new Thickness(10, 6),
            Background = AudioEditPalette.PaneAlt,
            Foreground = Brushes.White,
            BorderBrush = AudioEditPalette.Border,
            FontSize = 12,
        };
        Wire(btn, action);
        return btn;
    }

    void Wire(Button btn, Func<Task> action) =>
        btn.Click += async (_, _) =>
        {
            try { await action(); }
            catch (Exception ex) { Toast(ex.Message); }
        };
}

file static class MidiPianoUiExtensions
{
    public static T With<T>(this T control, Action<T> configure)
    {
        configure(control);
        return control;
    }
}
