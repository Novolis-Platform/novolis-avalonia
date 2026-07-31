namespace Novolis.Avalonia.Mobile;

/// <summary>Best-practice app-private storage roots (not shared Documents / SD card).</summary>
public interface IAppDataPaths
{
    /// <summary>Application product name used under the platform app-data root.</summary>
    string ProductName { get; }

    /// <summary>Root directory for this app's private data (created on demand).</summary>
    string RootDirectory { get; }

    /// <summary>Workspace mirror directory (e.g. books <c>content/</c> tree).</summary>
    string WorkspaceDirectory { get; }
}
