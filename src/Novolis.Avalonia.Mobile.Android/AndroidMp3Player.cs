using Android.Media;
using Android.OS;
using Java.IO;
using Novolis.Manuscript.Export.Audio;
using File = System.IO.File;
using Path = System.IO.Path;

namespace Novolis.Avalonia.Mobile.Android;

/// <summary>Plays Azure Speech MP3 bytes via Android <see cref="MediaPlayer"/> on the main looper.</summary>
public sealed class AndroidMp3Player : IAudioPlayer, IDisposable
{
    private readonly object gate = new();
    private readonly Handler main = new(Looper.MainLooper!);
    private MediaPlayer? player;
    private string? tempPath;
    private FileInputStream? stream;
    private CancellationTokenRegistration registration;

    /// <inheritdoc />
    public Task PlayAsync(byte[] mp3, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mp3);
        if (mp3.Length == 0)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        main.Post(() =>
        {
            try
            {
                PlayOnMain(mp3, tcs, cancellationToken);
            }
            catch (Exception ex)
            {
                CleanupPlayer();
                tcs.TrySetException(ex);
            }
        });

        return tcs.Task;
    }

    /// <inheritdoc />
    public void Stop()
    {
        if (Looper.MyLooper() == Looper.MainLooper)
            StopOnMain();
        else
        {
            using var done = new ManualResetEventSlim(false);
            main.Post(() =>
            {
                try
                {
                    StopOnMain();
                }
                finally
                {
                    done.Set();
                }
            });
            done.Wait(TimeSpan.FromSeconds(2));
        }
    }

    /// <inheritdoc />
    public void Dispose() => Stop();

    private void PlayOnMain(byte[] mp3, TaskCompletionSource tcs, CancellationToken cancellationToken)
    {
        StopOnMain();

        var cacheDir = global::Android.App.Application.Context?.CacheDir?.AbsolutePath
                       ?? Path.GetTempPath();
        Directory.CreateDirectory(cacheDir);
        var path = Path.Combine(cacheDir, $"novolis-speech-{Guid.NewGuid():N}.mp3");
        File.WriteAllBytes(path, mp3);
        var length = new FileInfo(path).Length;
        if (length <= 0)
        {
            TryDelete(path);
            tcs.TrySetException(new InvalidOperationException("Speech audio file was empty."));
            return;
        }

        var media = new MediaPlayer();
        media.Completion += (_, _) =>
        {
            CleanupPlayer();
            tcs.TrySetResult();
        };
        media.Error += (_, args) =>
        {
            CleanupPlayer();
            tcs.TrySetException(new InvalidOperationException(
                $"Android MediaPlayer failed (what={args?.What}, extra={args?.Extra})."));
        };

        var input = new FileInputStream(path);
        media.SetDataSource(input.FD, 0, length);
        media.Prepare();

        lock (gate)
        {
            player = media;
            tempPath = path;
            stream = input;
            registration = cancellationToken.Register(() => main.Post(StopOnMain));
        }

        media.Start();
    }

    private void StopOnMain()
    {
        lock (gate)
        {
            registration.Dispose();
            registration = default;
            CleanupPlayer();
        }
    }

    private void CleanupPlayer()
    {
        try
        {
            player?.Stop();
        }
        catch
        {
        }

        try
        {
            player?.Reset();
        }
        catch
        {
        }

        player?.Release();
        player = null;

        try
        {
            stream?.Close();
        }
        catch
        {
        }

        stream = null;

        if (tempPath is not null)
        {
            TryDelete(tempPath);
            tempPath = null;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }
}
