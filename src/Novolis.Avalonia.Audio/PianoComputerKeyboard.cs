using Avalonia.Input;

namespace Novolis.Avalonia.Audio;

/// <summary>
/// Computer-keyboard → MIDI map: ZXCVBNM whites + SD GHJ blacks; QWERTYU upper octave.
/// </summary>
public static class PianoComputerKeyboard
{
    /// <summary>Base MIDI for Z key when octave offset is 0 (C3).</summary>
    public const int BaseMidi = 48;

    /// <summary>Tries to map a key to a MIDI offset from <see cref="BaseMidi"/> (before octave shift).</summary>
    public static bool TryMap(Key key, out int semitoneOffset)
    {
        // Lower octave: Z row whites + S/D/G/H/J blacks
        //   S D   G H J
        // Z X C V B N M
        semitoneOffset = key switch
        {
            Key.Z => 0,   // C
            Key.S => 1,   // C#
            Key.X => 2,   // D
            Key.D => 3,   // D#
            Key.C => 4,   // E
            Key.V => 5,   // F
            Key.G => 6,   // F#
            Key.B => 7,   // G
            Key.H => 8,   // G#
            Key.N => 9,   // A
            Key.J => 10,  // A#
            Key.M => 11,  // B

            // Upper octave: Q row whites + 2/3/5/6/7 blacks
            //   2 3   5 6 7
            // Q W E R T Y U
            Key.Q => 12,
            Key.D2 => 13,
            Key.W => 14,
            Key.D3 => 15,
            Key.E => 16,
            Key.R => 17,
            Key.D5 => 18,
            Key.T => 19,
            Key.D6 => 20,
            Key.Y => 21,
            Key.D7 => 22,
            Key.U => 23,
            _ => -1,
        };
        return semitoneOffset >= 0;
    }

    public static int ToMidi(Key key, int octaveOffset)
    {
        if (!TryMap(key, out var semitone))
            return -1;
        return Math.Clamp(BaseMidi + octaveOffset + semitone, 0, 127);
    }
}
