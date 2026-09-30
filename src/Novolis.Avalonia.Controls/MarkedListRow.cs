using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Novolis.Avalonia.Controls;

/// <summary>A four-column list row: marker, leading, primary, trailing (all strings).</summary>
/// <param name="Marker">Optional dirty / status glyph (e.g. "*").</param>
/// <param name="Leading">Short leading label (e.g. number).</param>
/// <param name="Primary">Main title text.</param>
/// <param name="Trailing">Trailing meta (e.g. count).</param>
/// <param name="Tag">Optional payload for selection handlers.</param>
public sealed record MarkedListRow(
    string? Marker,
    string? Leading,
    string Primary,
    string? Trailing,
    object? Tag = null);
