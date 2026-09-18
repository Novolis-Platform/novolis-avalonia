using System.Diagnostics;
using System.Runtime.Versioning;
using Novolis.Avalonia.Diagnostics;
using Novolis.Logging.Diagnostics;

namespace Novolis.Avalonia.Mobile.Desktop;

/// <summary>Reveals the latest diagnostic file in Windows Explorer for sharing.</summary>
[SupportedOSPlatform("windows")]
public sealed class DesktopDiagnosticShare(IDiagnosticJournal journal) : IDiagnosticShare
{
    /// <inheritdoc />
    public string ActionLabel => "Open diagnostics";

    /// <inheritdoc />
    public Task ShareLatestAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = journal.GetRecentFiles().FirstOrDefault()
            ?? throw new InvalidOperationException("No diagnostics have been recorded yet.");
        var info = new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        Process.Start(info)?.Dispose();
        return Task.CompletedTask;
    }
}
