using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using Avalonia.Threading;
using Novolis.Transports.Torrent;
using Novolis.Transports.Torrent.TorrentEventArgs;

namespace Novolis.Avalonia.Torrent;

/// <summary>
///     Transfer-list row for one torrent — name, progress, peers, speeds (familiar client look).
/// </summary>
public sealed class TorrentProgressView : Border
{
    static IBrush Accent => Profile.AccentBrush;
    static IBrush Muted => Profile.MutedBrush;
    static IBrush RowBorder => Profile.BorderBrush;
    static IBrush SelectedBg => Profile.RaisedBrush;

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
        "Seeding" => Profile.AccentFillBrush,
        "Completed" => Profile.ActionSoftBrush,
        "Checking" => Profile.WarningBrush,
        "Stalled" => Profile.DangerBrush,
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
