using Microsoft.Extensions.DependencyInjection;
using Novolis.Avalonia.Diagnostics;
using Novolis.Avalonia.Mobile;
using Novolis.Logging.Diagnostics;

namespace Novolis.Avalonia.Mobile.Android;

/// <summary>DI registration for Android mobile platform services.</summary>
public static class AndroidMobileServiceCollectionExtensions
{
    /// <summary>
    /// Registers Android <see cref="ISecureTokenStore"/>, <see cref="IAppDataPaths"/>, <see cref="IBrowserLauncher"/>,
    /// and core device-flow presenter.
    /// </summary>
    public static IServiceCollection AddNovolisMobileAndroid(
        this IServiceCollection services,
        string productName = "BooksMobile")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);
        services.AddNovolisMobileCore();
        services.AddSingleton<ISecureTokenStore, AndroidSecureTokenStore>();
        services.AddSingleton<IAppDataPaths>(_ => new AndroidAppDataPaths(productName));
        services.AddSingleton<IBrowserLauncher, AndroidBrowserLauncher>();
        return services;
    }

    /// <summary>Registers Android diagnostic sharing for a journal created before host startup.</summary>
    public static IServiceCollection AddNovolisMobileAndroidDiagnostics(
        this IServiceCollection services,
        IDiagnosticJournal journal)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(journal);
        AndroidDiagnostics.InstallEarly(journal);
        services.AddSingleton<IDiagnosticShare, AndroidDiagnosticShare>();
        return services;
    }
}
