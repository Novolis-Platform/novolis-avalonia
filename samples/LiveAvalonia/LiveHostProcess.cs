using System.Diagnostics;

namespace LiveAvalonia;

internal sealed class LiveHostProcess : IAsyncDisposable
{
    private Process? _process;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_process is not null)
            return;

        var projectPath = ResolveHostProjectPath();
        var workingDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException($"Unable to resolve working directory for {projectPath}.");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add("Debug");

        _process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to launch Novolis.Audio.Live.Host.");

        _ = DrainAsync(_process.StandardOutput, Console.Out, cancellationToken);
        _ = DrainAsync(_process.StandardError, Console.Error, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        var process = Interlocked.Exchange(ref _process, null);
        if (process is null)
            return;

        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
        }

        try
        {
            await process.WaitForExitAsync().ConfigureAwait(false);
        }
        catch
        {
        }
        finally
        {
            process.Dispose();
        }
    }

    private static async Task DrainAsync(TextReader reader, TextWriter writer, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line is null)
                    break;

                await writer.WriteLineAsync($"[live-host] {line}").ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static string ResolveHostProjectPath()
    {
        var repoRoot = FindAncestorDirectory("novolis-avalonia");
        var audioRoot = Path.GetFullPath(Path.Combine(repoRoot, "..", "novolis-audio"));
        var projectPath = Path.Combine(audioRoot, "src", "Novolis.Audio.Live.Host", "Novolis.Audio.Live.Host.csproj");

        if (!File.Exists(projectPath))
            throw new FileNotFoundException("Unable to locate the Novolis Audio live host project.", projectPath);

        return projectPath;
    }

    private static string FindAncestorDirectory(string name)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (string.Equals(current.Name, name, StringComparison.OrdinalIgnoreCase))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException($"Unable to locate the '{name}' repository root from {AppContext.BaseDirectory}.");
    }
}
