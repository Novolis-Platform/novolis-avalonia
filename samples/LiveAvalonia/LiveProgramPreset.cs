using Novolis.Audio.Live;

namespace LiveAvalonia;

internal sealed record LiveProgramPreset(
    string Name,
    string Description,
    int Version,
    SwapPolicy SwapPolicy,
    TimeSpan DelayBeforeCompile,
    LiveProgramDefinition Definition);
