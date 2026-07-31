using Microsoft.Extensions.DependencyInjection;

namespace Novolis.Avalonia.Mobile;

/// <summary>DI registration for core mobile abstractions that have default implementations.</summary>
public static class MobileServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="BrowserDeviceFlowPresenter"/> as <see cref="IDeviceFlowPresenter"/>.
    /// Platform hosts must still register <see cref="ISecureTokenStore"/>, <see cref="IAppDataPaths"/>,
    /// and <see cref="IBrowserLauncher"/>.
    /// </summary>
    public static IServiceCollection AddNovolisMobileCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IDeviceFlowPresenter, BrowserDeviceFlowPresenter>();
        return services;
    }
}
