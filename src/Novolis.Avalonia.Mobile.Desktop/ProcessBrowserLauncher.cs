using System.Diagnostics;
using Novolis.Avalonia.Mobile;

namespace Novolis.Avalonia.Mobile.Desktop;

/// <summary>Opens URLs with the OS default browser.</summary>
public sealed class ProcessBrowserLauncher : IBrowserLauncher
{
    /// <inheritdoc />
    public Task OpenAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);
        cancellationToken.ThrowIfCancellationRequested();
        var psi = new ProcessStartInfo
        {
            FileName = uri.AbsoluteUri,
            UseShellExecute = true,
        };
        Process.Start(psi);
        return Task.CompletedTask;
    }
}
