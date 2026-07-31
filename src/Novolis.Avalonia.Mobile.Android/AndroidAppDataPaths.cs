using Android.Content;
using Novolis.Avalonia.Mobile;

namespace Novolis.Avalonia.Mobile.Android;

/// <summary>Private app <c>FilesDir/workspace</c> paths.</summary>
public sealed class AndroidAppDataPaths : IAppDataPaths
{
    /// <summary>Creates paths under the application files directory.</summary>
    public AndroidAppDataPaths(string productName = "BooksMobile")
        : this(
            global::Android.App.Application.Context
            ?? throw new InvalidOperationException("Android Application.Context is not available."),
            productName)
    {
    }

    /// <summary>Creates paths for the given context.</summary>
    public AndroidAppDataPaths(Context context, string productName = "BooksMobile")
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);
        ProductName = productName.Trim();
        var files = context.FilesDir?.AbsolutePath
            ?? throw new InvalidOperationException("Context.FilesDir is null.");
        RootDirectory = Path.Combine(files, ProductName);
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
