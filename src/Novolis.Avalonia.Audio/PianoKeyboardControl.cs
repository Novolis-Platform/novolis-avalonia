using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Novolis.Avalonia.Audio;

/// <summary>On-screen MIDI piano (white + black keys) with mouse note on/off.</summary>
public sealed class PianoKeyboardControl : Control
{
    const double WhiteKeyWidth = 28;
    const double WhiteKeyHeight = 120;
    const double BlackKeyWidth = 18;
    const double BlackKeyHeight = 72;

    static readonly bool[] IsBlack = [false, true, false, true, false, false, true, false, true, false, true, false];

    readonly HashSet<int> _pressed = [];
    int? _pointerNote;

    /// <summary>Lowest MIDI note drawn (inclusive). Default C3 = 48.</summary>
    public static readonly StyledProperty<int> LowestMidiProperty =
        AvaloniaProperty.Register<PianoKeyboardControl, int>(nameof(LowestMidi), 48);

    /// <summary>Number of white keys (each octave = 7).</summary>
    public static readonly StyledProperty<int> WhiteKeyCountProperty =
        AvaloniaProperty.Register<PianoKeyboardControl, int>(nameof(WhiteKeyCount), 21);

    public int LowestMidi
    {
        get => GetValue(LowestMidiProperty);
        set => SetValue(LowestMidiProperty, value);
    }

    public int WhiteKeyCount
    {
        get => GetValue(WhiteKeyCountProperty);
        set => SetValue(WhiteKeyCountProperty, value);
    }

    /// <summary>Raised when a key is pressed.</summary>
    public event Action<int>? NoteOn;

    /// <summary>Raised when a key is released.</summary>
    public event Action<int>? NoteOff;

    /// <summary>Highlights keys currently held (from session / computer keyboard).</summary>
    public void SetPressed(IEnumerable<int> midiNumbers)
    {
        _pressed.Clear();
        foreach (var n in midiNumbers)
            _pressed.Add(n);
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = WhiteKeyCount * WhiteKeyWidth;
        return new Size(width, WhiteKeyHeight);
    }

    public override void Render(DrawingContext context)
    {
        var whites = BuildWhiteNotes().ToArray();
        for (var i = 0; i < whites.Length; i++)
        {
            var midi = whites[i];
            var rect = new Rect(i * WhiteKeyWidth, 0, WhiteKeyWidth - 1, WhiteKeyHeight);
            var fill = _pressed.Contains(midi) ? AudioEditPalette.Accent : Brushes.WhiteSmoke;
            context.FillRectangle(fill, rect);
            context.DrawRectangle(new Pen(AudioEditPalette.Border, 1), rect);
        }

        foreach (var (midi, x) in BuildBlackKeys(whites))
        {
            var rect = new Rect(x - BlackKeyWidth / 2, 0, BlackKeyWidth, BlackKeyHeight);
            var fill = _pressed.Contains(midi)
                ? AudioEditPalette.Amber
                : new SolidColorBrush(Color.FromRgb(28, 32, 40));
            context.FillRectangle(fill, rect);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        e.Pointer.Capture(this);
        var note = HitTest(e.GetPosition(this));
        if (note is null)
            return;
        _pointerNote = note;
        _pressed.Add(note.Value);
        NoteOn?.Invoke(note.Value);
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        e.Pointer.Capture(null);
        ReleasePointerNote();
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        ReleasePointerNote();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_pointerNote is null || e.GetCurrentPoint(this).Properties.IsLeftButtonPressed == false)
            return;

        var note = HitTest(e.GetPosition(this));
        if (note is null || note == _pointerNote)
            return;

        _pressed.Remove(_pointerNote.Value);
        NoteOff?.Invoke(_pointerNote.Value);
        _pointerNote = note;
        _pressed.Add(note.Value);
        NoteOn?.Invoke(note.Value);
        InvalidateVisual();
    }

    void ReleasePointerNote()
    {
        if (_pointerNote is not { } note)
            return;
        _pointerNote = null;
        _pressed.Remove(note);
        NoteOff?.Invoke(note);
        InvalidateVisual();
    }

    int? HitTest(Point p)
    {
        var whites = BuildWhiteNotes().ToArray();
        foreach (var (midi, x) in BuildBlackKeys(whites))
        {
            var rect = new Rect(x - BlackKeyWidth / 2, 0, BlackKeyWidth, BlackKeyHeight);
            if (rect.Contains(p))
                return midi;
        }

        if (p.Y < 0 || p.Y > WhiteKeyHeight)
            return null;
        var index = (int)(p.X / WhiteKeyWidth);
        if (index < 0 || index >= whites.Length)
            return null;
        return whites[index];
    }

    IEnumerable<int> BuildWhiteNotes()
    {
        var midi = LowestMidi;
        // Snap down to nearest C if possible
        while (midi > 0 && midi % 12 != 0)
            midi--;

        var count = 0;
        while (count < WhiteKeyCount && midi <= 127)
        {
            if (!IsBlack[midi % 12])
            {
                yield return midi;
                count++;
            }

            midi++;
        }
    }

    static IEnumerable<(int Midi, double X)> BuildBlackKeys(IReadOnlyList<int> whites)
    {
        for (var i = 0; i < whites.Count; i++)
        {
            var midi = whites[i];
            var nextBlack = midi + 1;
            if (nextBlack > 127 || !IsBlack[nextBlack % 12])
                continue;
            // black sits between this white and the next white
            if (i + 1 >= whites.Count || whites[i + 1] != midi + 2)
                continue;
            yield return (nextBlack, (i + 1) * WhiteKeyWidth);
        }
    }
}
