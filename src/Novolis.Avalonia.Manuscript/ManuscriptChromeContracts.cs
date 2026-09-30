namespace Novolis.Avalonia.Manuscript;

/// <summary>Typed book selection for manuscript chrome panels.</summary>
public sealed record BookSelection(string? SeriesId, string BookId, string DirectoryPath);
