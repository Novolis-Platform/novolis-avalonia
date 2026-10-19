using System.Globalization;

namespace Novolis.Avalonia.Controls;

internal static class DownloadDisplayFormatter
{
    public static string FormatBytes(long? value)
    {
        if (value is null)
        {
            return "—";
        }

        var bytes = Math.Max(0, value.Value);
        string[] units = ["B", "KiB", "MiB", "GiB", "TiB"];
        var unitIndex = 0;
        var amount = (double)bytes;
        while (amount >= 1024 && unitIndex < units.Length - 1)
        {
            amount /= 1024;
            unitIndex++;
        }

        var format = unitIndex == 0 ? "0" : amount >= 100 ? "0" : "0.##";
        return $"{amount.ToString(format, CultureInfo.InvariantCulture)} {units[unitIndex]}";
    }

    public static string FormatSizePair(long? completed, long? total)
    {
        if (completed is null && total is null)
        {
            return "—";
        }

        return $"{FormatBytes(completed)} / {FormatBytes(total)}";
    }

    public static string FormatRate(long? bytesPerSecond)
    {
        return bytesPerSecond is null
            ? "—"
            : $"{FormatBytes(bytesPerSecond)}/s";
    }

    public static string FormatProgress(DownloadItem item)
    {
        if (item.ProgressFraction is not { } fraction)
        {
            return "—";
        }

        return $"{(fraction * 100).ToString("0.#", CultureInfo.InvariantCulture)}%";
    }

    public static string FormatPercent(double? value)
    {
        return value is { } percent
            ? $"{percent.ToString("0.#", CultureInfo.InvariantCulture)}%"
            : "—";
    }

    public static string FormatAvailability(DownloadMetadata metadata)
    {
        var percent = metadata.AvailabilityPercent is { } availability
            ? $"{availability.ToString("0.#", CultureInfo.InvariantCulture)}%"
            : "—";
        var sources = metadata.SourceCount is { } count
            ? $" · {count:N0} source{(count == 1 ? string.Empty : "s")}"
            : string.Empty;
        return percent + sources;
    }

    public static string FormatPeers(DownloadMetadata metadata)
    {
        var seeds = metadata.SeedCount?.ToString(CultureInfo.InvariantCulture) ?? "—";
        var peers = metadata.PeerCount?.ToString(CultureInfo.InvariantCulture) ?? "—";
        return $"{seeds} / {peers}";
    }

    public static string FormatEta(TimeSpan? eta)
    {
        if (eta is null)
        {
            return "—";
        }

        if (eta.Value == Timeout.InfiniteTimeSpan || eta.Value == TimeSpan.MaxValue)
        {
            return "Unknown";
        }

        var value = eta.Value < TimeSpan.Zero ? TimeSpan.Zero : eta.Value;
        return value.TotalDays >= 1
            ? $"{(int)value.TotalDays}d {value.Hours:00}h"
            : value.TotalHours >= 1
                ? $"{(int)value.TotalHours}h {value.Minutes:00}m"
                : $"{(int)value.TotalMinutes:00}:{value.Seconds:00}";
    }

    public static string FormatDate(DateTimeOffset? timestamp)
    {
        return timestamp?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) ?? "—";
    }

    public static string FormatList(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return "—";
        }

        const int maximumItems = 24;
        var visible = values.Take(maximumItems).ToArray();
        var text = string.Join(Environment.NewLine, visible);
        return values.Count > maximumItems
            ? $"{text}{Environment.NewLine}… and {values.Count - maximumItems:N0} more"
            : text;
    }

    public static string ValueOrPlaceholder(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "—" : value;
    }
}
