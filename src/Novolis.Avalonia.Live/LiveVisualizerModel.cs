using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Novolis.Audio.Live.Visuals;

namespace Novolis.Avalonia.Live;

public sealed record LiveVisualizerModel(
    LiveGraphNode? Graph,
    decimal Beat,
    int Bar,
    int Phrase,
    decimal Bpm,
    string? ActivePreset,
    string? SourceExcerpt);
