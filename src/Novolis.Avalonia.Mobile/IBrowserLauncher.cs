namespace Novolis.Avalonia.Mobile;

/// <summary>Opens an HTTPS URL in the system browser or Custom Tabs.</summary>
public interface IBrowserLauncher
{
    /// <summary>Launches <paramref name="uri"/> for the user (GitHub verify URL, etc.).</summary>
    Task OpenAsync(Uri uri, CancellationToken cancellationToken = default);
}
