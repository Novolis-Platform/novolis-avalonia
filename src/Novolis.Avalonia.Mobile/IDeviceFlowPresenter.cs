namespace Novolis.Avalonia.Mobile;

/// <summary>Shows GitHub device-flow user code and opens the verification URL.</summary>
public interface IDeviceFlowPresenter
{
    /// <summary>
    /// Presents <paramref name="userCode"/> to the user and opens <paramref name="verificationUri"/>.
    /// </summary>
    Task PresentAsync(string userCode, Uri verificationUri, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default presenter: opens the verification URI via <see cref="IBrowserLauncher"/>.
/// Host UI should display the user code while this opens the browser.
/// </summary>
public sealed class BrowserDeviceFlowPresenter : IDeviceFlowPresenter
{
    readonly IBrowserLauncher _browser;

    /// <summary>Creates a presenter that opens the verify URL.</summary>
    public BrowserDeviceFlowPresenter(IBrowserLauncher browser) =>
        _browser = browser ?? throw new ArgumentNullException(nameof(browser));

    /// <inheritdoc />
    public Task PresentAsync(string userCode, Uri verificationUri, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userCode);
        ArgumentNullException.ThrowIfNull(verificationUri);
        return _browser.OpenAsync(verificationUri, cancellationToken);
    }
}
