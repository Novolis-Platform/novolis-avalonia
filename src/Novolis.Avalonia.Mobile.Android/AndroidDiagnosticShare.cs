using Android.App;
using Android.Content;
using Android.Runtime;
using AndroidX.Core.Content;
using Novolis.Avalonia.Diagnostics;
using Novolis.Logging.Diagnostics;

namespace Novolis.Avalonia.Mobile.Android;

/// <summary>Captures Android managed failures and shares diagnostic files through the system chooser.</summary>
public static class AndroidDiagnostics
{
    private static IDiagnosticJournal? _journal;
    private static int _installed;

    /// <summary>Installs the Android runtime exception hook before host startup.</summary>
    public static void InstallEarly(IDiagnosticJournal journal)
    {
        ArgumentNullException.ThrowIfNull(journal);
        Interlocked.Exchange(ref _journal, journal);
        if (Interlocked.Exchange(ref _installed, 1) != 0)
            return;

        AndroidEnvironment.UnhandledExceptionRaiser += (_, args) =>
        {
            try
            {
                Volatile.Read(ref _journal)?.WriteException(
                    "AndroidEnvironment.UnhandledExceptionRaiser",
                    args.Exception);
            }
            catch
            {
                // Preserve Android's normal exception translation and termination path.
            }
        };
    }
}

/// <summary>Shares the latest diagnostic file using Android's system chooser.</summary>
public sealed class AndroidDiagnosticShare(IDiagnosticJournal journal) : IDiagnosticShare
{
    /// <inheritdoc />
    public string ActionLabel => "Share diagnostics";

    /// <inheritdoc />
    public Task ShareLatestAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = journal.GetRecentFiles().FirstOrDefault()
            ?? throw new InvalidOperationException("No diagnostics have been recorded yet.");
        var context = Application.Context
            ?? throw new InvalidOperationException("Android application context is unavailable.");
        var authority = $"{context.PackageName}.novolis.diagnostics";
        var file = new Java.IO.File(path);
        var uri = FileProvider.GetUriForFile(context, authority, file)
            ?? throw new InvalidOperationException("Unable to create a shareable diagnostic URI.");

        using var intent = new Intent(Intent.ActionSend)
            .SetType("application/x-ndjson")
            .PutExtra(Intent.ExtraStream, uri)
            .AddFlags(ActivityFlags.GrantReadUriPermission);
        intent.ClipData = ClipData.NewRawUri("diagnostics", uri);

        using var chooser = (Intent.CreateChooser(intent, "Share diagnostics")
            ?? throw new InvalidOperationException("Android could not create a diagnostic share chooser."))
            .AddFlags(ActivityFlags.NewTask);
        context.StartActivity(chooser);
        return Task.CompletedTask;
    }
}
