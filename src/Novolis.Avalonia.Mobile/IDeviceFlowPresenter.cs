namespace Novolis.Avalonia.Mobile;

/// <summary>Shows GitHub device-flow user code and opens the verification URL.</summary>
public interface IDeviceFlowPresenter
{
    /// <summary>
    /// Presents <paramref name="userCode"/> to the user and opens <paramref name="verificationUri"/>.
    /// </summary>
    Task PresentAsync(string userCode, Uri verificationUri, CancellationToken cancellationToken = default);
}
