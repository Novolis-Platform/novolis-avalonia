using Microsoft.Extensions.DependencyInjection;
using Novolis.Avalonia.Mobile;

namespace Novolis.Avalonia.Unit.Mobile;

public sealed class MobileCoreTests
{
    [Test]
    public async Task AddNovolisMobileCore_RegistersDeviceFlowPresenter()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBrowserLauncher, FakeBrowserLauncher>();
        services.AddNovolisMobileCore();

        await Assert.That(
            services.Any(d =>
                d.ServiceType == typeof(IDeviceFlowPresenter)
                && d.ImplementationType == typeof(BrowserDeviceFlowPresenter))).IsTrue();
    }

    [Test]
    public async Task BrowserDeviceFlowPresenter_OpensVerificationUri()
    {
        var browser = new FakeBrowserLauncher();
        var presenter = new BrowserDeviceFlowPresenter(browser);
        var uri = new Uri("https://github.com/login/device");

        await presenter.PresentAsync("ABCD-1234", uri);

        await Assert.That(browser.LastUri).IsEqualTo(uri);
    }

    private sealed class FakeBrowserLauncher : IBrowserLauncher
    {
        public Uri? LastUri { get; private set; }

        public Task OpenAsync(Uri uri, CancellationToken cancellationToken = default)
        {
            LastUri = uri;
            return Task.CompletedTask;
        }
    }
}
