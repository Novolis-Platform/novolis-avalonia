using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Novolis.Transports.Torrent;
using Novolis.Transports.Torrent.TorrentEventArgs;

namespace Novolis.Avalonia.Controls;

/// <summary>
///     Compact progress card for one torrent session snapshot.
/// </summary>
public sealed class TorrentProgressView : Border
{
    readonly TextBlock _title = new() { FontWeight = FontWeight.SemiBold, FontSize = 14 };
    readonly TextBlock _hash = new() { FontSize = 11, Opacity = 0.7, TextWrapping = TextWrapping.Wrap };
    readonly ProgressBar _bar = new() { Minimum = 0, Maximum = 100, Height = 10 };
    readonly TextBlock _pct = new() { FontSize = 12 };
    readonly TextBlock _speeds = new() { FontSize = 12 };
    readonly TextBlock _peers = new() { FontSize = 12 };
    readonly TextBlock _bytes = new() { FontSize = 12 };
    readonly TextBlock _duration = new() { FontSize = 12, Opacity = 0.8 };

    /// <summary>Creates an empty progress view.</summary>
    public TorrentProgressView()
    {
        Padding = new Thickness(12);
        BorderThickness = new Thickness(1);
        BorderBrush = new SolidColorBrush(Color.Parse("#3F3F46"));
        Background = new SolidColorBrush(Color.Parse("#252526"));
        CornerRadius = new CornerRadius(4);

        Child = new StackPanel
        {
            Spacing = 6,
            Children =
            {
                _title,
                _hash,
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                    Children =
                    {
                        Col(_bar, 0),
                        Col(_pct, 1)
                    }
                },
                _speeds,
                _peers,
                _bytes,
                _duration
            }
        };

        Clear();
    }

    /// <summary>Display title (usually torrent file name).</summary>
    public string Title
    {
        get => _title.Text ?? string.Empty;
        set => _title.Text = value;
    }

    /// <summary>Applies a progress snapshot.</summary>
    public void Apply(TorrentProgressInfo? info)
    {
        if (info is null)
        {
            Clear();
            return;
        }

        var pct = (double)info.CompletedPercentage;
        if (pct < 0) pct = 0;
        if (pct > 100) pct = 100;

        _hash.Text = $"infohash {info.TorrentInfoHash}";
        _bar.Value = pct;
        _pct.Text = $"  {pct:0.0}%";
        _speeds.Text = $"↓ {FormatRate(info.DownloadSpeed)}   ↑ {FormatRate(info.UploadSpeed)}";
        _peers.Text = $"peers  seeders {info.SeederCount} · leechers {info.LeecherCount} · connected {info.Peers.Count()}";
        _bytes.Text = $"bytes  down {FormatBytes(info.Downloaded)} · up {FormatBytes(info.Uploaded)}";
        _duration.Text = $"elapsed {FormatDuration(info.Duration)}";
    }

    /// <summary>Resets to idle placeholders.</summary>
    public void Clear()
    {
        _hash.Text = "no torrent loaded";
        _bar.Value = 0;
        _pct.Text = "  —";
        _speeds.Text = "↓ —   ↑ —";
        _peers.Text = "peers  —";
        _bytes.Text = "bytes  —";
        _duration.Text = "elapsed —";
    }

    static Control Col(Control c, int column)
    {
        Grid.SetColumn(c, column);
        return c;
    }

    static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        double v = bytes;
        string[] units = ["KB", "MB", "GB", "TB"];
        var i = -1;
        do
        {
            v /= 1024;
            i++;
        } while (v >= 1024 && i < units.Length - 1);

        return $"{v:0.##} {units[i]}";
    }

    static string FormatRate(decimal bytesPerSecond) => $"{FormatBytes((long)bytesPerSecond)}/s";

    static string FormatDuration(TimeSpan t)
    {
        if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h {t.Minutes:D2}m {t.Seconds:D2}s";
        if (t.TotalMinutes >= 1) return $"{t.Minutes}m {t.Seconds:D2}s";
        return $"{t.Seconds}s";
    }
}

/// <summary>
///     End-to-end torrent session control: pick .torrent, start/stop client, live progress.
/// </summary>
public sealed class TorrentSessionPanel : Border, IDisposable
{
    readonly TextBox _downloadDir;
    readonly NumericUpDown _port;
    readonly TextBlock _status;
    readonly TorrentProgressView _progress = new();
    readonly Button _browseTorrent;
    readonly Button _browseDir;
    readonly Button _start;
    readonly Button _stop;
    readonly DispatcherTimer _timer;

    TorrentClient? _client;
    TorrentInfo? _torrent;
    string? _torrentPath;
    bool _disposed;

    /// <summary>Creates a session panel with default download directory under the user profile.</summary>
    public TorrentSessionPanel()
    {
        var defaultDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Novolis", "TorrentLab", "downloads");
        Directory.CreateDirectory(defaultDir);

        Padding = new Thickness(12);
        Background = new SolidColorBrush(Color.Parse("#1E1E1E"));

        _downloadDir = new TextBox { Text = defaultDir, PlaceholderText = "Download directory" };
        _port = new NumericUpDown
        {
            Minimum = 1024,
            Maximum = 65535,
            Value = 6881,
            Width = 140,
            MinWidth = 140,
            FormatString = "0",
            Increment = 1
        };
        _status = new TextBlock { Text = "Idle — open a .torrent to begin.", Opacity = 0.85, TextWrapping = TextWrapping.Wrap };

        _browseTorrent = new Button { Content = "Open .torrent…", Padding = new Thickness(10, 4) };
        _browseDir = new Button { Content = "Browse…", Padding = new Thickness(10, 4) };
        _start = new Button { Content = "Start", Padding = new Thickness(14, 4), IsEnabled = false };
        _stop = new Button { Content = "Stop", Padding = new Thickness(14, 4), IsEnabled = false };

        _browseTorrent.Click += async (_, _) => await PickTorrentAsync();
        _browseDir.Click += async (_, _) => await PickDirectoryAsync();
        _start.Click += (_, _) => StartSession();
        _stop.Click += (_, _) => StopSession();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += (_, _) => RefreshProgress();

        var dirRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Children =
            {
                Col(_downloadDir, 0),
                Col(_browseDir, 1)
            }
        };
        _browseDir.Margin = new Thickness(8, 0, 0, 0);

        var portRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Listen port", VerticalAlignment = VerticalAlignment.Center },
                _port
            }
        };

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { _browseTorrent, _start, _stop }
        };

        Child = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = "Torrent session", FontSize = 16, FontWeight = FontWeight.SemiBold },
                dirRow,
                portRow,
                actions,
                _progress,
                _status
            }
        };
    }

    /// <summary>Current torrent metadata, if loaded.</summary>
    public TorrentInfo? Torrent => _torrent;

    /// <summary>Path of the loaded .torrent file.</summary>
    public string? TorrentPath => _torrentPath;

    /// <summary>Raised after progress polling.</summary>
    public event Action<TorrentProgressInfo?>? ProgressUpdated;

    /// <summary>Loads a .torrent from disk without starting download.</summary>
    public bool TryLoadTorrent(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!TorrentInfo.TryLoad(path, out var info) || info is null)
        {
            SetStatus($"Failed to parse torrent: {path}");
            return false;
        }

        _torrent = info;
        _torrentPath = path;
        _progress.Title = info.Files.FirstOrDefault()?.FilePath
                          ?? Path.GetFileName(path);
        _progress.Clear();
        _hashPreview(info);
        _start.IsEnabled = true;
        SetStatus($"Loaded {Path.GetFileName(path)} · {FormatBytes(info.Length)} · {info.PiecesCount} pieces · {info.AnnounceList.Count()} trackers");
        return true;
    }

    /// <summary>Starts the client and begins the loaded torrent.</summary>
    public void StartSession()
    {
        if (_torrent is null)
        {
            SetStatus("Open a .torrent first.");
            return;
        }

        var dir = _downloadDir.Text?.Trim();
        if (string.IsNullOrWhiteSpace(dir))
        {
            SetStatus("Download directory is required.");
            return;
        }

        Directory.CreateDirectory(dir);
        var port = (int)(_port.Value ?? 6881);
        if (port < 1024 || port > 65535)
        {
            port = 6881;
            _port.Value = port;
            SetStatus("Listen port reset to 6881 (must be 1024–65535).");
        }

        // Dogfood: if the payload already exists next to the .torrent (or under samples),
        // stage it into the download dir so hashing becomes a local seed instead of a barren swarm.
        var staged = TryStageLocalPayload(dir);
        if (staged is not null)
            SetStatus($"Staged local payload → {staged} (this torrent has no public seeders).");

        try
        {
            StopSession();
            _client = new TorrentClient(port, dir);
            _client.TorrentStarted += (_, e) => Dispatcher.UIThread.Post(() =>
                SetStatus($"Started {e.TorrentInfo.InfoHash[..Math.Min(8, e.TorrentInfo.InfoHash.Length)]}… listening :{port}"));
            _client.TorrentStopped += (_, _) => Dispatcher.UIThread.Post(() => SetStatus("Torrent stopped."));
            _client.TorrentSeeding += (_, _) => Dispatcher.UIThread.Post(() =>
                SetStatus($"Seeding on :{port} — file complete. Public trackers won't help this private sample torrent."));
            _client.Start();
            _client.Start(_torrent);
            _start.IsEnabled = false;
            _stop.IsEnabled = true;
            _browseTorrent.IsEnabled = false;
            _timer.Start();
            SetStatus(staged is null
                ? $"Downloading to {dir} on :{port} — note: this sample infohash has no public swarm; stage Core-current.iso beside the .torrent to seed locally."
                : $"Hashing/seeding staged ISO on :{port}");
            RefreshProgress();
        }
        catch (Exception ex)
        {
            SetStatus($"Start failed: {ex.Message}");
            StopSession();
        }
    }

    string? TryStageLocalPayload(string downloadDir)
    {
        if (_torrent is null || string.IsNullOrWhiteSpace(_torrentPath))
            return null;

        var relative = _torrent.Files.FirstOrDefault()?.FilePath;
        if (string.IsNullOrWhiteSpace(relative))
            return null;

        var fileName = Path.GetFileName(relative);
        var dest = Path.Combine(downloadDir, relative);

        var torrentDir = Path.GetDirectoryName(_torrentPath)!;
        var candidates = new[]
        {
            Path.Combine(torrentDir, fileName),
            Path.Combine(torrentDir, relative),
            Path.Combine(AppContext.BaseDirectory, "samples", fileName),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "samples", fileName)),
        };

        foreach (var src in candidates)
        {
            if (!File.Exists(src)) continue;
            if (new FileInfo(src).Length != _torrent.Length) continue;

            // Same length is not enough: CreateFile pre-allocates zeros that hash ~0–3%.
            if (File.Exists(dest)
                && new FileInfo(dest).Length == _torrent.Length
                && FilesLikelyIdentical(src, dest))
                return dest;

            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(src, dest, overwrite: true);
            return dest;
        }

        return File.Exists(dest) && new FileInfo(dest).Length == _torrent.Length ? dest : null;
    }

    static bool FilesLikelyIdentical(string a, string b)
    {
        var fa = new FileInfo(a);
        var fb = new FileInfo(b);
        if (fa.Length != fb.Length) return false;
        // Quick content check: first + middle + last 64 KiB (enough to reject zero-filled stubs).
        const int chunk = 64 * 1024;
        using var sa = File.OpenRead(a);
        using var sb = File.OpenRead(b);
        return ChunkEqual(sa, sb, 0, chunk)
               && ChunkEqual(sa, sb, Math.Max(0, fa.Length / 2 - chunk / 2), chunk)
               && ChunkEqual(sa, sb, Math.Max(0, fa.Length - chunk), chunk);
    }

    static bool ChunkEqual(Stream a, Stream b, long offset, int length)
    {
        length = (int)Math.Min(length, a.Length - offset);
        if (length <= 0) return true;
        var ba = new byte[length];
        var bb = new byte[length];
        a.Position = offset;
        b.Position = offset;
        a.ReadExactly(ba);
        b.ReadExactly(bb);
        return ba.AsSpan().SequenceEqual(bb);
    }

    /// <summary>Stops the active session.</summary>
    public void StopSession()
    {
        _timer.Stop();
        if (_client is not null)
        {
            try
            {
                if (_torrent is not null && _client.IsRunning)
                    _client.Stop(_torrent.InfoHash);
                if (_client.IsRunning)
                    _client.Stop();
            }
            catch
            {
                // best-effort shutdown
            }

            _client.Dispose();
            _client = null;
        }

        _start.IsEnabled = _torrent is not null;
        _stop.IsEnabled = false;
        _browseTorrent.IsEnabled = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopSession();
        _timer.Stop();
    }

    async Task PickTorrentAsync()
    {
        var top = TopLevel.GetTopLevel(this);
        if (top?.StorageProvider is null) return;

        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open BitTorrent file",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Torrent")
                {
                    Patterns = ["*.torrent"]
                }
            ]
        });

        var file = files.FirstOrDefault();
        if (file?.TryGetLocalPath() is { } path)
            TryLoadTorrent(path);
    }

    async Task PickDirectoryAsync()
    {
        var top = TopLevel.GetTopLevel(this);
        if (top?.StorageProvider is null) return;

        var folders = await top.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Download directory",
            AllowMultiple = false
        });

        var folder = folders.FirstOrDefault();
        if (folder?.TryGetLocalPath() is { } path)
            _downloadDir.Text = path;
    }

    void RefreshProgress()
    {
        if (_client is null || _torrent is null || !_client.IsRunning)
            return;

        try
        {
            var info = _client.GetProgressInfo(_torrent.InfoHash);
            _progress.Apply(info);
            ProgressUpdated?.Invoke(info);
            if (info.CompletedPercentage >= 100m)
                SetStatus($"Complete — {FormatBytes(info.Downloaded)} saved under {_downloadDir.Text}");
        }
        catch (Exception ex)
        {
            SetStatus($"Progress error: {ex.Message}");
        }
    }

    void _hashPreview(TorrentInfo info)
    {
        // nudge progress header with hash until first poll
        _progress.Apply(new TorrentProgressInfo(
            info.InfoHash,
            TimeSpan.Zero,
            0,
            0,
            0,
            0,
            0,
            0,
            0));
    }

    void SetStatus(string text) => _status.Text = text;

    static Control Col(Control c, int column)
    {
        Grid.SetColumn(c, column);
        return c;
    }

    static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        double v = bytes;
        string[] units = ["KB", "MB", "GB", "TB"];
        var i = -1;
        do
        {
            v /= 1024;
            i++;
        } while (v >= 1024 && i < units.Length - 1);

        return $"{v:0.##} {units[i]}";
    }
}
