namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// One searchable browser entry.
/// </summary>
public sealed record AssetStudioBrowserEntry(
    string Kind,
    string Id,
    string DisplayName,
    string Description);
