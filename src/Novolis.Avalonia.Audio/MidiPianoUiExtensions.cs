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

file static class MidiPianoUiExtensions
{
    public static T With<T>(this T control, Action<T> configure)
    {
        configure(control);
        return control;
    }
}
