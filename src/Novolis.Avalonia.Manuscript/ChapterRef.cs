namespace Novolis.Avalonia.Manuscript;

/// <summary>Typed chapter reference for manuscript chrome panels.</summary>
public sealed record ChapterRef(string FilePath, string Label, double SortKey);
