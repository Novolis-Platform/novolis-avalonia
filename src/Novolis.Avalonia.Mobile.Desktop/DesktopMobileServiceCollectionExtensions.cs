using System.Runtime.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Novolis.Avalonia.Diagnostics;
using Novolis.Avalonia.Mobile;
using Novolis.Logging.Diagnostics;

namespace Novolis.Avalonia.Mobile.Desktop;

/// <summary>DI registration for Windows desktop mobile platform services.</summary>
public static class DesktopMobileServiceCollectionExtensions
{
    /// <summary>
    /// Registers desktop <see cref="ISecureTokenStore"/>, <see cref="IAppDataPaths"/>, <see cref="IBrowserLauncher"/>,
    /// and core device-flow presenter.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static IServiceCollection AddNovolisMobileDesktop(
        this IServiceCollection services,
        string productName = "BooksMobile")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);
        services.AddNovolisMobileCore();
        services.AddSingleton<ISecureTokenStore, WindowsCredentialTokenStore>();
        services.AddSingleton<IAppDataPaths>(_ => new DesktopAppDataPaths(productName));
        services.AddSingleton<IBrowserLauncher, ProcessBrowserLauncher>();
        return services;
    }

    /// <summary>Registers a Windows action that reveals the latest diagnostic file for sharing.</summary>
    [SupportedOSPlatform("windows")]
    public static IServiceCollection AddNovolisMobileDesktopDiagnostics(
        this IServiceCollection services,
        IDiagnosticJournal journal)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(journal);
        services.AddSingleton<IDiagnosticShare, DesktopDiagnosticShare>();
        return services;
    }
}
