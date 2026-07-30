using Novolis.Audio.Live;

namespace Novolis.Avalonia.Live;

/// <summary>
/// A live demo document: editable source is the source of truth for the studio.
/// </summary>
public sealed record LiveDemoDocument(
    string Id,
    string Title,
    string Description,
    string Source,
    SwapPolicy SwapPolicy,
    TimeSpan DelayBeforeCompile);
