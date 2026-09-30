using Avalonia;
using Avalonia.Threading;
using Novolis.Logging.Diagnostics;

namespace Novolis.Avalonia.Diagnostics;

/// <summary>Platform-specific user action that exports or reveals recent app diagnostics.</summary>
public interface IDiagnosticShare
{
    /// <summary>Label appropriate for a platform's diagnostic action.</summary>
    string ActionLabel { get; }

    /// <summary>Exports or reveals the most recent diagnostic file.</summary>
    Task ShareLatestAsync(CancellationToken cancellationToken = default);
}
