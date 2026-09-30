using Avalonia.Controls;

namespace Novolis.Avalonia.Briefing;

/// <summary>One keyed metric row for <see cref="MetricTableView"/>.</summary>
public sealed class MetricRow
{
    /// <summary>Creates a metric row.</summary>
    public MetricRow(string key, string value, string? note = null)
    {
        Key = key;
        Value = value;
        Note = note ?? string.Empty;
    }

    /// <summary>Row key / name.</summary>
    public string Key { get; }

    /// <summary>Primary value.</summary>
    public string Value { get; }

    /// <summary>Optional note column.</summary>
    public string Note { get; }
}
