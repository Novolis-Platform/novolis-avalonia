using Avalonia;
using Avalonia.Threading;
using Novolis.Logging.Diagnostics;

namespace Novolis.Avalonia.Diagnostics;

/// <summary>Installs process and Avalonia exception hooks into an app-private diagnostic journal.</summary>
public static class AvaloniaDiagnostics
{
    private static IDiagnosticJournal? _journal;
    private static int _processHooksInstalled;
    private static int _dispatcherHookInstalled;

    /// <summary>
    /// Installs process-wide hooks. Call this as the first operation in the platform entry point,
    /// before building the dependency-injection host.
    /// </summary>
    public static void InstallEarly(IDiagnosticJournal journal)
    {
        ArgumentNullException.ThrowIfNull(journal);
        Interlocked.Exchange(ref _journal, journal);
        if (Interlocked.Exchange(ref _processHooksInstalled, 1) != 0)
            return;

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            WriteException("AppDomain.UnhandledException", args.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            WriteException("TaskScheduler.UnobservedTaskException", args.Exception);
            args.SetObserved();
        };
    }

    /// <summary>Installs the Avalonia UI-thread exception hook after framework setup.</summary>
    public static AppBuilder UseNovolisDiagnostics(this AppBuilder builder, IDiagnosticJournal journal)
    {
        ArgumentNullException.ThrowIfNull(builder);
        InstallEarly(journal);
        return builder.AfterSetup(_ =>
        {
            if (Interlocked.Exchange(ref _dispatcherHookInstalled, 1) != 0)
                return;

            Dispatcher.UIThread.UnhandledException += (_, args) =>
                WriteException("Avalonia.Dispatcher.UnhandledException", args.Exception);
        });
    }

    private static void WriteException(string source, object? exceptionObject)
    {
        try
        {
            var exception = exceptionObject as Exception
                ?? new Exception(exceptionObject?.ToString() ?? "Unknown exception.");
            Volatile.Read(ref _journal)?.WriteException(source, exception);
        }
        catch
        {
            // The runtime must remain free to finish its normal failure path.
        }
    }
}

/// <summary>Platform-specific user action that exports or reveals recent app diagnostics.</summary>
public interface IDiagnosticShare
{
    /// <summary>Label appropriate for a platform's diagnostic action.</summary>
    string ActionLabel { get; }

    /// <summary>Exports or reveals the most recent diagnostic file.</summary>
    Task ShareLatestAsync(CancellationToken cancellationToken = default);
}
