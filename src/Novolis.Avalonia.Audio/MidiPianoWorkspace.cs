using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Novolis.Audio.Core;
using Novolis.Audio.Edit;
using Novolis.Audio.Midi;

namespace Novolis.Avalonia.Audio;

/// <summary>
/// MIDI piano workspace: instrument browser, keyboard, record, save/load patches and MIDI.
/// </summary>
public sealed class MidiPianoWorkspace : Border, IDisposable
{
    static readonly (Key Key, int MidiOffset)[] ComputerKeys =
    [
        (Key.A, 0), (Key.W, 1), (Key.S, 2), (Key.E, 3), (Key.D, 4),
        (Key.F, 5), (Key.T, 6), (Key.G, 7), (Key.Y, 8), (Key.H, 9),
        (Key.U, 10), (Key.J, 11), (Key.K, 12),
    ];

    readonly TextBlock _title = new()
    {
        FontSize = 22,
        FontFamily = new FontFamily("Segoe UI Semibold"),
        Foreground = Brushes.White,
        Text = "MIDI Piano",
    };
    readonly TextBlock _status = new() { Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap };
    readonly TextBlock _patchLabel = new() { Foreground = AudioEditPalette.Amber };
    readonly MidiPreviewMixer _mixer = new();
    readonly InstrumentBrowserControl _browser = new();
    readonly PianoKeyboardControl _keyboard = new() { LowestMidi = 48, WhiteKeyCount = 21 };
    readonly HashSet<Key> _keysDown = [];
    int _octaveOffset;
    bool _disposed;

    /// <summary>Creates a piano workspace. Optional <paramref name="musicProject"/> receives bounced WAV assets.</summary>
    public MidiPianoWorkspace(MidiPianoSession? session = null, MusicProject? musicProject = null)
    {
        MusicProject = musicProject;
        Session = session ?? new MidiPianoSession(
            format: musicProject?.Format ?? new PcmFormat(44_100, Channels: 1, PcmSampleFormat.Int16));
        Focusable = true;

        _browser.Bind(Session.Bank, Session.SelectedPatch.Id);
        _browser.PatchSelected += patch =>
        {
            Session.SelectPatch(patch);
            RefreshStatus();
        };
        _keyboard.NoteOn += OnKeyboardNoteOn;
        _keyboard.NoteOff += OnKeyboardNoteOff;
        Session.Changed += () =>
        {
            _keyboard.SetPressed(Session.HeldMidiNumbers);
            RefreshStatus();
        };

        Background = AudioEditPalette.Pane;
        Child = BuildLayout();
        RefreshStatus();
        AttachedToVisualTree += (_, _) => Focus();
    }

    /// <summary>Piano session state.</summary>
    public MidiPianoSession Session { get; }

    /// <summary>Optional arrangement project to receive bounced takes.</summary>
    public MusicProject? MusicProject { get; }

    /// <summary>Header title override.</summary>
    public string HeaderTitle
    {
        get => _title.Text ?? "";
        set => _title.Text = value;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Z)
        {
            _octaveOffset = Math.Max(-24, _octaveOffset - 12);
            RefreshStatus();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.X)
        {
            _octaveOffset = Math.Min(36, _octaveOffset + 12);
            RefreshStatus();
            e.Handled = true;
            return;
        }

        foreach (var (key, offset) in ComputerKeys)
        {
            if (e.Key != key || !_keysDown.Add(key))
                continue;
            var midi = 60 + _octaveOffset + offset;
            if (midi is >= 0 and <= 127)
                OnKeyboardNoteOn(midi);
            e.Handled = true;
            return;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        foreach (var (key, offset) in ComputerKeys)
        {
            if (e.Key != key || !_keysDown.Remove(key))
                continue;
            var midi = 60 + _octaveOffset + offset;
            if (midi is >= 0 and <= 127)
                OnKeyboardNoteOff(midi);
            e.Handled = true;
            return;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Session.AllNotesOff();
        _mixer.Dispose();
    }

    void OnKeyboardNoteOn(int midi)
    {
        var pcm = Session.NoteOn(midi);
        _mixer.Play(pcm);
        _keyboard.SetPressed(Session.HeldMidiNumbers);
        RefreshStatus();
    }

    void OnKeyboardNoteOff(int midi)
    {
        Session.NoteOff(midi);
        _keyboard.SetPressed(Session.HeldMidiNumbers);
        RefreshStatus();
    }

    Control BuildLayout()
    {
        var toolbar = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 10),
        };
        void AddBtn(string label, Func<Task> action) =>
            toolbar.Children.Add(MakeButton(label, action));

        AddBtn("Record / Stop", async () =>
        {
            if (Session.IsRecording)
                Session.StopRecording();
            else
                Session.StartRecording();
            RefreshStatus();
            await Task.CompletedTask;
        });
        AddBtn("Play take", async () =>
        {
            Session.StopRecording();
            if (Session.Sequence.Notes.Count == 0)
            {
                _status.Text = "Nothing recorded yet.";
                return;
            }

            _mixer.Play(Session.RenderSequence());
            await Task.CompletedTask;
        });
        AddBtn("Clear take", async () =>
        {
            Session.Sequence.Clear();
            RefreshStatus();
            await Task.CompletedTask;
        });
        AddBtn("Save MIDI…", SaveMidiAsync);
        AddBtn("Load MIDI…", LoadMidiAsync);
        AddBtn("Save patch…", SavePatchAsync);
        AddBtn("Load patch…", LoadPatchAsync);
        AddBtn("Save bank…", SaveBankAsync);
        AddBtn("Import bank…", ImportBankAsync);
        AddBtn("Bounce WAV to library", BounceToLibraryAsync);

        var scrollKeys = new ScrollViewer
        {
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = new Border
            {
                Background = AudioEditPalette.PaneAlt,
                BorderBrush = AudioEditPalette.Border,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8),
                Child = _keyboard,
            },
        };

        return new DockPanel
        {
            Margin = new Thickness(16),
            LastChildFill = true,
            Children =
            {
                new StackPanel
                {
                    Spacing = 4,
                    [DockPanel.DockProperty] = Dock.Top,
                    Children = { _title, _patchLabel, _status, toolbar },
                },
                new Border
                {
                    [DockPanel.DockProperty] = Dock.Bottom,
                    Margin = new Thickness(0, 12, 0, 0),
                    Child = new StackPanel
                    {
                        Spacing = 6,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "Keyboard — click keys, or A–K (W/E/T/Y/U black). Z/X octave down/up.",
                                Foreground = Brushes.White,
                            },
                            scrollKeys,
                        },
                    },
                },
                _browser,
            },
        };
    }

    async Task SaveMidiAsync()
    {
        var path = await PickSaveAsync("MIDI sequence", "mid");
        if (path is null)
            return;
        Session.Sequence.Title = Path.GetFileNameWithoutExtension(path);
        Session.SaveMidi(path);
        _status.Text = $"Saved MIDI → {path}";
    }

    async Task LoadMidiAsync()
    {
        var path = await PickOpenAsync("MIDI sequence", "mid");
        if (path is null)
            return;
        Session.LoadMidi(path);
        _browser.Bind(Session.Bank, Session.SelectedPatch.Id);
        _status.Text = $"Loaded MIDI ← {path} ({Session.Sequence.Notes.Count} notes)";
    }

    async Task SavePatchAsync()
    {
        var path = await PickSaveAsync("Instrument patch", "json");
        if (path is null)
            return;
        Session.SaveSelectedPatch(path);
        _status.Text = $"Saved patch → {path}";
    }

    async Task LoadPatchAsync()
    {
        var path = await PickOpenAsync("Instrument patch", "json");
        if (path is null)
            return;
        Session.LoadPatchIntoBank(path);
        _browser.Bind(Session.Bank, Session.SelectedPatch.Id);
        _status.Text = $"Loaded patch ← {path}";
    }

    async Task SaveBankAsync()
    {
        var path = await PickSaveAsync("Instrument bank", "json");
        if (path is null)
            return;
        Session.SaveBank(path);
        _status.Text = $"Saved bank ({Session.Bank.Patches.Count} sounds) → {path}";
    }

    async Task ImportBankAsync()
    {
        var path = await PickOpenAsync("Instrument bank", "json");
        if (path is null)
            return;
        Session.ImportBank(path);
        _browser.Bind(Session.Bank, Session.SelectedPatch.Id);
        _status.Text = $"Imported bank ← {path} (now {Session.Bank.Patches.Count} sounds)";
    }

    async Task BounceToLibraryAsync()
    {
        if (MusicProject is null)
        {
            _status.Text = "No arrangement project attached — save MIDI/WAV from file buttons instead.";
            return;
        }

        if (Session.Sequence.Notes.Count == 0)
        {
            // Bounce a short demo chord of the current sound
            var demo = new MidiSequence("Patch Preview");
            demo.Add(new MidiNoteEvent(60, 100, TimeSpan.Zero, TimeSpan.FromMilliseconds(400)));
            demo.Add(new MidiNoteEvent(64, 100, TimeSpan.FromMilliseconds(80), TimeSpan.FromMilliseconds(400)));
            demo.Add(new MidiNoteEvent(67, 100, TimeSpan.FromMilliseconds(160), TimeSpan.FromMilliseconds(400)));
            var pcm = MidiSynth.RenderSequence(Session.Format, Session.SelectedPatch, demo);
            AudioEditOps.AddPcm(MusicProject, $"{Session.SelectedPatch.Name} preview", pcm);
            _status.Text = $"Bounced {Session.SelectedPatch.Name} preview into the sound library.";
            return;
        }

        var take = Session.RenderSequence();
        AudioEditOps.AddPcm(MusicProject, Session.Sequence.Title, take);
        _status.Text = $"Bounced take “{Session.Sequence.Title}” into the sound library.";
        await Task.CompletedTask;
    }

    async Task<string?> PickSaveAsync(string title, string ext)
    {
        if (TopLevel.GetTopLevel(this) is not { StorageProvider: { } sp })
            return null;
        var file = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            DefaultExtension = ext,
            SuggestedFileName = ext == "mid" ? "take.mid" : "sounds.json",
            FileTypeChoices =
            [
                new FilePickerFileType(title) { Patterns = [$"*.{ext}"] },
            ],
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
            FileTypeFilter =
            [
                new FilePickerFileType(title) { Patterns = [$"*.{ext}"] },
            ],
        });
        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    Button MakeButton(string label, Func<Task> action)
    {
        var btn = new Button
        {
            Content = label,
            Margin = new Thickness(0, 0, 8, 8),
            Padding = new Thickness(12, 6),
            Background = AudioEditPalette.PaneAlt,
            Foreground = Brushes.White,
            BorderBrush = AudioEditPalette.Border,
        };
        btn.Click += async (_, _) =>
        {
            try { await action(); }
            catch (Exception ex) { _status.Text = ex.Message; }
        };
        return btn;
    }

    void RefreshStatus()
    {
        _patchLabel.Text =
            $"{Session.SelectedPatch.Category} · {Session.SelectedPatch.Name}  ({Session.SelectedPatch.Id})";
        var rec = Session.IsRecording ? "● REC" : "○ idle";
        var oct = 4 + _octaveOffset / 12;
        _status.Text =
            $"{rec}  ·  {Session.Sequence.Notes.Count} notes in take  ·  bank {Session.Bank.Patches.Count} sounds  ·  computer octave C{oct}  ·  Z/X shift octave";
    }
}
