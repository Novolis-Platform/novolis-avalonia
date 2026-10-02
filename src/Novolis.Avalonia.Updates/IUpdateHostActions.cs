using Novolis.Registry.Updates;

namespace Novolis.Avalonia.Updates;

/// <summary>Host seams for browser, notification, Downloads, and installer behavior.</summary>
public interface IUpdateHostActions
{
    /// <summary>Shows a lightweight notification.</summary>
    ValueTask ShowToastAsync(UpdateSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>Shows a more prominent update prompt.</summary>
    ValueTask ShowPopupAsync(UpdateSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>Opens the public GitHub release page.</summary>
    ValueTask OpenReleaseAsync(Uri releaseUri, CancellationToken cancellationToken = default);

    /// <summary>Reveals a verified downloaded artifact in the file manager.</summary>
    ValueTask RevealDownloadAsync(string path, CancellationToken cancellationToken = default);
}
