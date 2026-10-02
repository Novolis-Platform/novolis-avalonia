using Android.Content;
using Novolis.Avalonia.Mobile;
using Novolis.IO.Platform.Android;

namespace Novolis.Avalonia.Mobile.Android;

/// <summary>Default Android app paths from <see cref="AndroidAppStorage"/>.</summary>
public sealed class AndroidAppDataPaths : IAppDataPaths
{
    /// <summary>Creates paths for the running application.</summary>
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
        var locations = AndroidAppStorage.Open(context, productName);
        ProductName = locations.ProductName;
        RootDirectory = locations.RootDirectory;
        WorkspaceDirectory = locations.WorkspaceDirectory;
        CacheDirectory = locations.CacheDirectory;
    }

    /// <inheritdoc />
    public string ProductName { get; }

    /// <inheritdoc />
    public string RootDirectory { get; }

    /// <inheritdoc />
    public string WorkspaceDirectory { get; }

    /// <inheritdoc />
    public string CacheDirectory { get; }
}
