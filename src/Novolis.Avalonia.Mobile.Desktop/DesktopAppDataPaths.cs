using Novolis.Avalonia.Mobile;

namespace Novolis.Avalonia.Mobile.Desktop;

/// <summary>LocalAppData paths under <c>%LocalAppData%\Novolis\{product}\</c>.</summary>
public sealed class DesktopAppDataPaths : IAppDataPaths
{
    /// <summary>Creates paths for <paramref name="productName"/> (e.g. <c>BooksMobile</c>).</summary>
    public DesktopAppDataPaths(string productName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);
        ProductName = productName.Trim();
        RootDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Novolis",
            ProductName);
        WorkspaceDirectory = Path.Combine(RootDirectory, "workspace");
        Directory.CreateDirectory(WorkspaceDirectory);
    }

    /// <inheritdoc />
    public string ProductName { get; }

    /// <inheritdoc />
    public string RootDirectory { get; }

    /// <inheritdoc />
    public string WorkspaceDirectory { get; }
}
