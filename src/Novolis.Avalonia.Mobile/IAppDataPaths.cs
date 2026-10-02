namespace Novolis.Avalonia.Mobile;

/// <summary>Default app-data roots for a product. Android may use shared Documents when that tree is writable.</summary>
public interface IAppDataPaths
{
    /// <summary>Application product name used under the platform app-data root.</summary>
    string ProductName { get; }

    /// <summary>Root directory for this app's private data (created on demand).</summary>
    string RootDirectory { get; }

    /// <summary>Workspace mirror directory (e.g. books <c>content/</c> tree).</summary>
    string WorkspaceDirectory { get; }
}
