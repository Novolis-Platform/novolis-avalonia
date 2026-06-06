using Novolis.Audio.Live;
using Novolis.Audio.MusicTheory;
using Novolis.Audio.Patterns;

namespace LiveAvalonia;

internal static class LiveSamplePrograms
{
    public static LiveProgramDefinition CreateProgram(int version)
    {
        var lead = CreateLeadPattern(version);
        var bass = CreateBassPattern(version);
        var rhythm = CreateRhythmPattern(version);

        return new LiveProgramDefinition(
            Bpm: 120m,
            Tracks:
            [
                new TrackDefinition("lead", InstrumentKind.Sine, lead),
                new TrackDefinition("bass", InstrumentKind.Saw, bass, Channel: 1),
                new TrackDefinition("pulse", InstrumentKind.Square, rhythm, Channel: 2),
            ],
            Root: new LayerPattern([lead, bass, rhythm]));
    }

    private static PatternNode CreateLeadPattern(int version)
    {
        PatternNode motif =
            new SequencePattern(
            [
                new NotePattern(new Note(new Pitch(PitchClass.C, Octave.MiddleC), Duration.Quarter, Velocity.Default, InstrumentKind.Sine)),
                new NotePattern(new Note(new Pitch(PitchClass.D, Octave.MiddleC), Duration.Quarter, Velocity.Default, InstrumentKind.Sine)),
                new NotePattern(new Note(new Pitch(PitchClass.E, Octave.MiddleC), Duration.Quarter, Velocity.Default, InstrumentKind.Sine)),
                new RestPattern(Duration.Quarter),
            ]);

        return version switch
        {
            1 => motif,
            2 => new TransposePattern(motif, 2),
            _ => new TransposePattern(motif, version),
        };
    }

    private static PatternNode CreateBassPattern(int version)
    {
        var root = version % 2 == 0 ? PitchClass.F : PitchClass.C;

        return new RepeatPattern(
            new SequencePattern(
            [
                new NotePattern(new Note(new Pitch(root, Octave.MiddleC), Duration.Half, Velocity.Default, InstrumentKind.Saw)),
                new RestPattern(Duration.Half),
            ]),
            2);
    }

    private static PatternNode CreateRhythmPattern(int version)
    {
        var accent = version == 1 ? Velocity.Default : new Velocity(112);

        return new LayerPattern(
        [
            new RepeatPattern(new NotePattern(new Note(new Pitch(PitchClass.C, Octave.MiddleC), Duration.Eighth, accent, InstrumentKind.Square)), 4),
            new RepeatPattern(new RestPattern(Duration.Eighth), 4),
        ]);
    }
}
