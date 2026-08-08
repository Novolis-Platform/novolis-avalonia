using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Novolis.Transports.Torrent;
using Novolis.Transports.Torrent.TorrentEventArgs;

namespace Novolis.Avalonia.Torrent;

/// <summary>
///     Transfer-list row for one torrent — name, progress, peers, speeds (familiar client look).
/// </summary>
public sealed class TorrentProgressView : Border
{
    static readonly IBrush Accent = new SolidColorBrush(Color.Parse("#2A9D8F"));
    static readonly IBrush Muted = new SolidColorBrush(Color.Parse("#A1A1AA"));
    static readonly IBrush RowBorder = new SolidColorBrush(Color.Parse("#2E3A4A"));
    static readonly IBrush SelectedBg = new SolidColorBrush(Color.Parse("#243447"));

    readonly TextBlock _name = new() { FontWeight = FontWeight.SemiBold, FontSize = 13, TextTrimming = TextTrimming.CharacterEllipsis };
    readonly TextBlock _size = new() { FontSize = 12, Foreground = Muted };
    readonly ProgressBar _bar = new()
    {
        Minimum = 0,
        Maximum = 100,
        Height = 8,
        MinWidth = 120,
        Foreground = Accent
    };
    readonly TextBlock _pct = new() { FontSize = 12, Width = 52, TextAlignment = TextAlignment.Right };
    readonly TextBlock _status = new() { FontSize = 12, FontWeight = FontWeight.Medium };
    readonly TextBlock _seeds = new() { FontSize = 12, Width = 56, TextAlignment = TextAlignment.Right };
    readonly TextBlock _peers = new() { FontSize = 12, Width = 56, TextAlignment = TextAlignment.Right };
    readonly TextBlock _down = new() { FontSize = 12, Width = 78, TextAlignment = TextAlignment.Right };
    readonly TextBlock _up = new() { FontSize = 12, Width = 78, TextAlignment = TextAlignment.Right };
    readonly TextBlock _eta = new() { FontSize = 12, Width = 64, TextAlignment = TextAlignment.Right, Foreground = Muted };

    /// <summary>Creates an empty transfer row.</summary>
    public TorrentProgressView()
    {
        Padding = new Thickness(10, 8);
        BorderThickness = new Thickness(1);
        BorderBrush = RowBorder;
        Background = SelectedBg;
        CornerRadius = new CornerRadius(3);
        Cursor = new Cursor(StandardCursorType.Hand);

        var progressCell = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            MinWidth = 140,
            Children =
            {
                Col(_bar, 0),
                Col(_pct, 1)
            }
        };
        _pct.Margin = new Thickness(6, 0, 0, 0);
        _bar.VerticalAlignment = VerticalAlignment.Center;

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,88,160,100,56,56,78,78,64"),
            ColumnSpacing = 8,
            Children =
            {
                Col(StackNameSize(), 0),
                Col(_size, 1),
                Col(progressCell, 2),
                Col(_status, 3),
                Col(_seeds, 4),
                Col(_peers, 5),
                Col(_down, 6),
                Col(_up, 7),
                Col(_eta, 8)
            }
        };

        foreach (var child in new Control[] { _size, _status, _seeds, _peers, _down, _up, _eta })
            child.VerticalAlignment = VerticalAlignment.Center;

        Child = row;
        Clear();
    }

    /// <summary>Display title (usually torrent file name).</summary>
    public string Title
    {
        get => _name.Text ?? string.Empty;
        set => _name.Text = value;
    }

    /// <summary>Total size label when torrent metadata is known.</summary>
    public void SetTorrentMeta(long length, int pieceCount)
    {
        _size.Text = FormatBytes(length);
        ToolTip.SetTip(this, $"{pieceCount} pieces · {FormatBytes(length)}");
    }

    /// <summary>Applies a progress snapshot.</summary>
    public void Apply(TorrentProgressInfo? info, string? forcedStatus = null)
    {
        if (info is null)
        {
            Clear(keepTitle: true);
            if (!string.IsNullOrEmpty(forcedStatus))
                _status.Text = forcedStatus;
            return;
        }

        var pct = ClampPct((double)info.CompletedPercentage);
        _bar.Value = pct;
        _pct.Text = $"{pct:0.0}%";
        _seeds.Text = info.SeederCount.ToString();
        _peers.Text = info.LeecherCount.ToString();
        _down.Text = FormatRate(info.DownloadSpeed);
        _up.Text = FormatRate(info.UploadSpeed);
        _eta.Text = FormatEta(info);
        _status.Text = forcedStatus ?? InferStatus(info);
        _status.Foreground = StatusBrush(_status.Text);
    }

    /// <summary>Resets to idle placeholders.</summary>
    public void Clear(bool keepTitle = false)
    {
        if (!keepTitle)
            _name.Text = "No torrent loaded";
        _size.Text = "—";
        _bar.Value = 0;
        _pct.Text = "—";
        _status.Text = "Idle";
        _status.Foreground = Muted;
        _seeds.Text = "—";
        _peers.Text = "—";
        _down.Text = "—";
        _up.Text = "—";
        _eta.Text = "—";
    }

    Control StackNameSize() => new StackPanel
    {
        Spacing = 2,
        VerticalAlignment = VerticalAlignment.Center,
        Children =
        {
            _name,
            new TextBlock
            {
                Text = "Selected transfer",
                FontSize = 10,
                Foreground = Muted
            }
        }
    };

    static string InferStatus(TorrentProgressInfo info)
    {
        if (info.CompletedPercentage >= 99.9m)
            return info.UploadSpeed > 0 ? "Seeding" : "Completed";
        if (info.DownloadSpeed > 0)
            return "Downloading";
        if (info.SeederCount + info.LeecherCount == 0)
            return "Stalled";
        return "Downloading";
    }

    static IBrush StatusBrush(string? status) => status switch
    {
        "Downloading" => Accent,
        "Seeding" => new SolidColorBrush(Color.Parse("#5B9BD5")),
        "Completed" => new SolidColorBrush(Color.Parse("#6FCF97")),
        "Checking" => new SolidColorBrush(Color.Parse("#E9C46A")),
        "Stalled" => new SolidColorBrush(Color.Parse("#E07A5F")),
        "Stopped" => Muted,
        _ => Muted
    };

    static double ClampPct(double pct)
    {
        if (pct < 0) return 0;
        if (pct > 100) return 100;
        return pct;
    }

    static string FormatEta(TorrentProgressInfo info)
    {
        if (info.CompletedPercentage >= 99.9m) return "Done";
        if (info.DownloadSpeed <= 0) return "∞";
        // Rough remaining from percentage (length unknown here) — show elapsed instead as activity signal.
        return FormatDuration(info.Duration);
    }

    static Control Col(Control c, int column)
    {
        Grid.SetColumn(c, column);
        return c;
    }

    internal static string FormatBytes(long bytes)
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

    internal static string FormatRate(decimal bytesPerSecond) => $"{FormatBytes((long)bytesPerSecond)}/s";

    internal static string FormatDuration(TimeSpan t)
    {
        if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h {t.Minutes:D2}m";
        if (t.TotalMinutes >= 1) return $"{t.Minutes}m {t.Seconds:D2}s";
        return $"{t.Seconds}s";
    }
}

/// <summary>
///     Familiar torrent-client session: toolbar, transfer list, detail tabs, status bar.
/// </summary>
public sealed class TorrentSessionPanel : Border, IDisposable
{
    static readonly IBrush Bg = new SolidColorBrush(Color.Parse("#121820"));
    static readonly IBrush Panel = new SolidColorBrush(Color.Parse("#182230"));
    static readonly IBrush BorderTone = new SolidColorBrush(Color.Parse("#2E3A4A"));
    static readonly IBrush Muted = new SolidColorBrush(Color.Parse("#A1A1AA"));
    static readonly IBrush Accent = new SolidColorBrush(Color.Parse("#2A9D8F"));

    readonly TextBox _downloadDir;
    readonly NumericUpDown _port;
    readonly TextBlock _status;
    readonly TextBlock _statusDown;
    readonly TextBlock _statusUp;
    readonly TextBlock _statusPort;
    readonly TextBlock _generalBody;
    readonly TextBlock _trackersBody;
    readonly TextBlock _filesBody;
    readonly TorrentProgressView _progress = new();
    readonly Button _browseTorrent;
    readonly Button _browseDir;
    readonly Button _openFolder;
    readonly Button _start;
    readonly Button _stop;
    readonly DispatcherTimer _timer;

    TorrentClient? _client;
    TorrentInfo? _torrent;
    string? _torrentPath;
    bool _disposed;
    string _sessionState = "Idle";

    /// <summary>Creates a session panel with default download directory under the user profile.</summary>
    public TorrentSessionPanel()
    {
        var defaultDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Novolis", "TorrentLab", "downloads");
        Directory.CreateDirectory(defaultDir);

        Background = Bg;
        Padding = new Thickness(0);

        _downloadDir = new TextBox
        {
            Text = defaultDir,
            PlaceholderText = "Save path",
            MinHeight = 28
        };
        _port = new NumericUpDown
        {
            Minimum = 1024,
            Maximum = 65535,
            Value = 6881,
            Width = 100,
            MinWidth = 100,
            FormatString = "0",
            Increment = 1
        };

        _browseTorrent = ToolButton("Add torrent…");
        _browseDir = ToolButton("Browse…");
        _openFolder = ToolButton("Open folder");
        _start = ToolButton("Start");
        _stop = ToolButton("Stop");
        _start.IsEnabled = false;
        _stop.IsEnabled = false;
        _openFolder.IsEnabled = true;

        _browseTorrent.Click += async (_, _) => await PickTorrentAsync();
        _browseDir.Click += async (_, _) => await PickDirectoryAsync();
        _openFolder.Click += (_, _) => OpenDownloadFolder();
        _start.Click += (_, _) => StartSession();
        _stop.Click += (_, _) =>
        {
            StopSession();
            SetSessionState("Stopped");
            SetStatus("Transfer stopped.");
            RefreshDetails(null);
        };

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _timer.Tick += (_, _) => RefreshProgress();

        _generalBody = DetailBody("Load a .torrent to see details.");
        _trackersBody = DetailBody("No trackers yet.");
        _filesBody = DetailBody("No files yet.");

        _status = new TextBlock
        {
            Text = "Ready — add a torrent or load the Core sample.",
            FontSize = 12,
            Foreground = Muted,
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        };
        _statusDown = new TextBlock { Text = "↓ 0 B/s", FontSize = 12, Width = 88 };
        _statusUp = new TextBlock { Text = "↑ 0 B/s", FontSize = 12, Width = 88 };
        _statusPort = new TextBlock { Text = "Port 6881", FontSize = 12, Foreground = Muted, Width = 88 };

        Child = new DockPanel();
        var root = (DockPanel)Child;

        var statusBar = BuildStatusBar();
        DockPanel.SetDock(statusBar, Dock.Bottom);
        root.Children.Add(statusBar);

        var toolbar = BuildToolbar();
        DockPanel.SetDock(toolbar, Dock.Top);
        root.Children.Add(toolbar);

        var options = BuildOptions();
        DockPanel.SetDock(options, Dock.Top);
        root.Children.Add(options);

        var listHeader = BuildListHeader();
        DockPanel.SetDock(listHeader, Dock.Top);
        root.Children.Add(listHeader);

        var transfer = new Border
        {
            Margin = new Thickness(12, 0, 12, 8),
            Child = _progress
        };
        DockPanel.SetDock(transfer, Dock.Top);
        root.Children.Add(transfer);

        var tabs = BuildTabs();
        tabs.Margin = new Thickness(12, 0, 12, 8);
        root.Children.Add(tabs);
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
        var name = info.Files.FirstOrDefault()?.FilePath ?? Path.GetFileName(path);
        _progress.Title = name;
        _progress.SetTorrentMeta(info.Length, info.PiecesCount);
        _progress.Clear(keepTitle: true);
        SetSessionState("Stopped");
        _progress.Apply(null, "Stopped");
        _start.IsEnabled = true;
        RefreshDetails(null);
        SetStatus($"Added “{name}” · {TorrentProgressView.FormatBytes(info.Length)} · {info.PiecesCount} pieces · {info.AnnounceList.Count()} trackers");
        return true;
    }

    /// <summary>Starts the client and begins the loaded torrent.</summary>
    public void StartSession()
    {
        if (_torrent is null)
        {
            SetStatus("Add a .torrent first.");
            return;
        }

        var dir = _downloadDir.Text?.Trim();
        if (string.IsNullOrWhiteSpace(dir))
        {
            SetStatus("Save path is required.");
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

        _statusPort.Text = $"Port {port}";

        try
        {
            StopSession();

            string? staged;
            try
            {
                staged = TryStageLocalPayload(dir);
            }
            catch (Exception ex)
            {
                SetStatus($"Could not stage local payload: {ex.Message}");
                return;
            }

            _client = new TorrentClient(port, dir);
            _client.TorrentStarted += (_, e) => Dispatcher.UIThread.Post(() =>
            {
                SetSessionState("Checking");
                SetStatus($"Started {ShortHash(e.TorrentInfo.InfoHash)} — checking pieces on :{port}");
            });
            _client.TorrentStopped += (_, _) => Dispatcher.UIThread.Post(() =>
            {
                SetSessionState("Stopped");
                SetStatus("Torrent stopped.");
            });
            _client.TorrentSeeding += (_, _) => Dispatcher.UIThread.Post(() =>
            {
                SetSessionState("Seeding");
                SetStatus($"Seeding on :{port} — transfer complete.");
            });
            _client.Start();
            _client.Start(_torrent);
            _start.IsEnabled = false;
            _stop.IsEnabled = true;
            _browseTorrent.IsEnabled = false;
            _timer.Start();
            SetSessionState(staged is null ? "Downloading" : "Checking");
            SetStatus(staged is null
                ? $"Downloading to {dir} on :{port}"
                : $"Checking local data on :{port}");
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
        _statusDown.Text = "↓ 0 B/s";
        _statusUp.Text = "↑ 0 B/s";
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
            Title = "Add torrent",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Torrent") { Patterns = ["*.torrent"] },
                new FilePickerFileType("All files") { Patterns = ["*.*"] }
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
            Title = "Save path",
            AllowMultiple = false
        });

        var folder = folders.FirstOrDefault();
        if (folder?.TryGetLocalPath() is { } path)
            _downloadDir.Text = path;
    }

    void OpenDownloadFolder()
    {
        var dir = _downloadDir.Text?.Trim();
        if (string.IsNullOrWhiteSpace(dir))
        {
            SetStatus("Save path is empty.");
            return;
        }

        Directory.CreateDirectory(dir);
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = dir,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            SetStatus($"Could not open folder: {ex.Message}");
        }
    }

    void RefreshProgress()
    {
        if (_client is null || _torrent is null || !_client.IsRunning)
            return;

        try
        {
            var info = _client.GetProgressInfo(_torrent.InfoHash);
            var status = _sessionState;
            if (info is not null)
            {
                if (info.CompletedPercentage >= 99.9m)
                    status = info.UploadSpeed > 0 ? "Seeding" : "Completed";
                else if (info.DownloadSpeed > 0)
                    status = "Downloading";
                else if (_sessionState is "Checking" && info.CompletedPercentage > 0 && info.CompletedPercentage < 99.9m)
                    status = "Checking";
                else if (info.SeederCount + info.LeecherCount == 0 && info.DownloadSpeed <= 0)
                    status = info.CompletedPercentage > 3 ? "Downloading" : "Stalled";

                _statusDown.Text = $"↓ {TorrentProgressView.FormatRate(info.DownloadSpeed)}";
                _statusUp.Text = $"↑ {TorrentProgressView.FormatRate(info.UploadSpeed)}";
            }

            SetSessionState(status);
            _progress.Apply(info, status);
            RefreshDetails(info);
            ProgressUpdated?.Invoke(info);

            if (info is { CompletedPercentage: >= 100m })
                SetStatus($"Completed — saved under {_downloadDir.Text}");
        }
        catch (Exception ex)
        {
            SetStatus($"Progress error: {ex.Message}");
        }
    }

    void RefreshDetails(TorrentProgressInfo? info)
    {
        if (_torrent is null)
        {
            _generalBody.Text = "Load a .torrent to see details.";
            _trackersBody.Text = "No trackers yet.";
            _filesBody.Text = "No files yet.";
            return;
        }

        var name = _torrent.Files.FirstOrDefault()?.FilePath ?? Path.GetFileName(_torrentPath) ?? "torrent";
        var pct = info?.CompletedPercentage ?? 0m;
        var lines = new List<string>
        {
            $"Name: {name}",
            $"Save path: {_downloadDir.Text}",
            $"Infohash: {_torrent.InfoHash}",
            $"Size: {TorrentProgressView.FormatBytes(_torrent.Length)}",
            $"Pieces: {_torrent.PiecesCount} × {TorrentProgressView.FormatBytes(_torrent.PieceLength)}",
            $"Progress: {pct:0.0}%",
            $"Status: {_sessionState}",
            $"Elapsed: {(info is null ? "—" : TorrentProgressView.FormatDuration(info.Duration))}",
            $"Downloaded: {(info is null ? "—" : TorrentProgressView.FormatBytes(info.Downloaded))}",
            $"Uploaded: {(info is null ? "—" : TorrentProgressView.FormatBytes(info.Uploaded))}",
            $"Ratio: {FormatRatio(info)}",
            $"Seeds / peers: {(info is null ? "— / —" : $"{info.SeederCount} / {info.LeecherCount}")}",
        };
        if (!string.IsNullOrWhiteSpace(_torrentPath))
            lines.Insert(1, $"Torrent file: {_torrentPath}");
        _generalBody.Text = string.Join(Environment.NewLine, lines);

        var trackers = _torrent.AnnounceList.Select(u => u.AbsoluteUri).Distinct().ToList();
        _trackersBody.Text = trackers.Count == 0
            ? "No announce URLs in this torrent."
            : string.Join(Environment.NewLine, trackers.Select((u, i) => $"{i + 1}. {u}"));

        var files = _torrent.Files.ToList();
        _filesBody.Text = files.Count == 0
            ? "No files listed."
            : string.Join(Environment.NewLine,
                files.Select(f => $"{f.FilePath}  ·  {TorrentProgressView.FormatBytes(f.Length)}"));
    }

    static string FormatRatio(TorrentProgressInfo? info)
    {
        if (info is null || info.Downloaded <= 0) return "—";
        return (info.Uploaded / (decimal)info.Downloaded).ToString("0.000");
    }

    void SetSessionState(string state)
    {
        _sessionState = state;
    }

    void SetStatus(string text) => _status.Text = text;

    static string ShortHash(string hash) =>
        hash.Length <= 8 ? hash : hash[..8] + "…";

    static Button ToolButton(string content) => new()
    {
        Content = content,
        Padding = new Thickness(12, 6),
        MinHeight = 30
    };

    static TextBlock DetailBody(string text) => new()
    {
        Text = text,
        FontSize = 12,
        Foreground = Muted,
        TextWrapping = TextWrapping.Wrap,
        FontFamily = new FontFamily("Consolas, Cascadia Mono, Courier New, monospace")
    };

    static TextBlock HeaderCell(string text, int col)
    {
        var block = new TextBlock
        {
            Text = text,
            FontSize = 11,
            FontWeight = FontWeight.SemiBold,
            Foreground = Muted,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(block, col);
        return block;
    }

    Border BuildToolbar()
    {
        var bar = new Border
        {
            Background = Panel,
            BorderBrush = BorderTone,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(12, 8),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    _browseTorrent,
                    _start,
                    _stop,
                    new Border { Width = 1, Background = BorderTone, Margin = new Thickness(4, 2) },
                    _openFolder
                }
            }
        };
        return bar;
    }

    Border BuildOptions()
    {
        var saveLabel = new TextBlock
        {
            Text = "Save path",
            FontSize = 12,
            Foreground = Muted,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 72
        };
        var portLabel = new TextBlock
        {
            Text = "Port",
            FontSize = 12,
            Foreground = Muted,
            VerticalAlignment = VerticalAlignment.Center
        };

        var saveRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            ColumnSpacing = 8,
            Children =
            {
                Col(saveLabel, 0),
                Col(_downloadDir, 1),
                Col(_browseDir, 2)
            }
        };

        var portRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0),
            Children = { portLabel, _port }
        };

        return new Border
        {
            Background = Panel,
            BorderBrush = BorderTone,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(12, 10),
            Child = new StackPanel { Children = { saveRow, portRow } }
        };
    }

    Border BuildListHeader()
    {
        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,88,160,100,56,56,78,78,64"),
            ColumnSpacing = 8,
            Children =
            {
                HeaderCell("Name", 0),
                HeaderCell("Size", 1),
                HeaderCell("Progress", 2),
                HeaderCell("Status", 3),
                HeaderCell("Seeds", 4),
                HeaderCell("Peers", 5),
                HeaderCell("Down", 6),
                HeaderCell("Up", 7),
                HeaderCell("ETA", 8)
            }
        };

        return new Border
        {
            Margin = new Thickness(12, 10, 12, 4),
            Padding = new Thickness(10, 4),
            Child = header
        };
    }

    TabControl BuildTabs()
    {
        ScrollViewer Wrap(Control body) => new()
        {
            Content = new Border
            {
                Padding = new Thickness(12),
                Background = Panel,
                Child = body
            }
        };

        return new TabControl
        {
            Items =
            {
                new TabItem { Header = "General", Content = Wrap(_generalBody) },
                new TabItem { Header = "Trackers", Content = Wrap(_trackersBody) },
                new TabItem { Header = "Files", Content = Wrap(_filesBody) }
            }
        };
    }

    Border BuildStatusBar()
    {
        return new Border
        {
            Background = Panel,
            BorderBrush = BorderTone,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(12, 6),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto,Auto"),
                ColumnSpacing = 12,
                Children =
                {
                    Col(_status, 0),
                    Col(_statusDown, 1),
                    Col(_statusUp, 2),
                    Col(_statusPort, 3)
                }
            }
        };
    }

    static Control Col(Control c, int column)
    {
        Grid.SetColumn(c, column);
        return c;
    }
}
