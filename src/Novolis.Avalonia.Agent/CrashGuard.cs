using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Threading;

namespace Novolis.Avalonia.Agent;

/// <summary>
/// Process crash guard: dump log (+ Windows minidump), open Notepad, and keep Avalonia alive
/// by marking dispatcher / task exceptions as handled.
/// </summary>
public static class CrashGuard
{
    static int _openedEditor;
    static string _appName = "Novolis.Avalonia";
    static bool _installed;

    public static string CrashDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Novolis",
            Sanitize(_appName),
            "crashes");

    /// <summary>Install AppDomain + TaskScheduler handlers. Call once at process start.</summary>
    public static void Install(string appName)
    {
        if (string.IsNullOrWhiteSpace(appName))
            throw new ArgumentException("App name required.", nameof(appName));

        _appName = appName.Trim();
        if (_installed)
            return;
        _installed = true;

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var ex = e.ExceptionObject as Exception
                     ?? new Exception(e.ExceptionObject?.ToString() ?? "Unknown unhandled exception");
            Report(ex, "AppDomain.UnhandledException", openEditor: true, writeMiniDump: true);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Report(e.Exception, "TaskScheduler.UnobservedTaskException", openEditor: true, writeMiniDump: false);
            e.SetObserved();
        };
    }

    /// <summary>UI-thread faults: log + Notepad, mark handled so the window stays up.</summary>
    public static void InstallAvalonia(Dispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        dispatcher.UnhandledException += (_, e) =>
        {
            Report(e.Exception, "Dispatcher.UnhandledException", openEditor: true, writeMiniDump: false);
            e.Handled = true;
        };

        dispatcher.UnhandledExceptionFilter += (_, e) =>
        {
            // Request that the exception be raised to UnhandledException (then we handle it).
            e.RequestCatch = true;
        };
    }

    /// <summary>Full report: .log (+ .dmp on Windows when requested) and open Notepad once.</summary>
    public static string Report(
        Exception exception,
        string source,
        bool openEditor = true,
        bool writeMiniDump = false)
    {
        ArgumentNullException.ThrowIfNull(exception);
        try
        {
            Directory.CreateDirectory(CrashDirectory);
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
            var logPath = Path.Combine(CrashDirectory, $"crash-{stamp}.log");
            File.WriteAllText(logPath, FormatLog(exception, source));

            if (writeMiniDump && OperatingSystem.IsWindows())
            {
                var dumpPath = Path.Combine(CrashDirectory, $"crash-{stamp}.dmp");
                try
                {
                    MiniDump.TryWrite(dumpPath);
                    File.AppendAllText(logPath, Environment.NewLine + $"MiniDump: {dumpPath}" + Environment.NewLine);
                }
                catch (Exception dumpEx)
                {
                    File.AppendAllText(logPath, Environment.NewLine + $"MiniDump failed: {dumpEx}" + Environment.NewLine);
                }
            }

            if (openEditor && Interlocked.Exchange(ref _openedEditor, 1) == 0)
                OpenInNotepad(logPath);

            return logPath;
        }
        catch
        {
            // Last resort — never throw from the crash path.
            return string.Empty;
        }
    }

    /// <summary>Log without Notepad spam (agent IPC / recoverable faults).</summary>
    public static string ReportSilent(Exception exception, string source) =>
        Report(exception, source, openEditor: false, writeMiniDump: false);

    static string FormatLog(Exception exception, string source)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{_appName} crash ({source})");
        sb.AppendLine($"UTC: {DateTime.UtcNow:O}");
        sb.AppendLine($"PID: {Environment.ProcessId}");
        sb.AppendLine($"Machine: {Environment.MachineName}");
        sb.AppendLine($"OS: {Environment.OSVersion}");
        sb.AppendLine($"Runtime: {Environment.Version}");
        sb.AppendLine($"BaseDirectory: {AppContext.BaseDirectory}");
        sb.AppendLine($"CommandLine: {Environment.CommandLine}");
        sb.AppendLine();
        sb.AppendLine(exception.ToString());
        if (exception is AggregateException agg)
        {
            sb.AppendLine();
            sb.AppendLine("--- Flattened ---");
            foreach (var inner in agg.Flatten().InnerExceptions)
                sb.AppendLine(inner.ToString());
        }

        return sb.ToString();
    }

    static void OpenInNotepad(string path)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "notepad.exe",
                    Arguments = Quote(path),
                    UseShellExecute = true,
                });
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
        }
        catch
        {
            // ignored — log file remains on disk
        }
    }

    static string Quote(string path) =>
        path.Contains(' ', StringComparison.Ordinal) ? $"\"{path}\"" : path;

    static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}

internal static class MiniDump
{
    const int MiniDumpWithFullMemoryInfo = 0x00000800;
    const int MiniDumpWithThreadInfo = 0x00001000;
    const int MiniDumpNormal = 0x00000000;

    public static bool TryWrite(string path)
    {
        if (!OperatingSystem.IsWindows())
            return false;

        using var fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        var proc = Process.GetCurrentProcess();
        return MiniDumpWriteDump(
            proc.Handle,
            (uint)proc.Id,
            fs.SafeFileHandle,
            MiniDumpNormal | MiniDumpWithThreadInfo | MiniDumpWithFullMemoryInfo,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);
    }

    [DllImport("dbghelp.dll", SetLastError = true)]
    static extern bool MiniDumpWriteDump(
        IntPtr hProcess,
        uint processId,
        SafeHandle hFile,
        int dumpType,
        IntPtr exceptionParam,
        IntPtr userStreamParam,
        IntPtr callbackParam);
}
