using Avalonia.Controls;

namespace Novolis.Avalonia.Briefing;

/// <summary>Read-only DataGrid for keyed briefing metrics.</summary>
public sealed class MetricTableView : DataGrid
{
    /// <summary>Creates a three-column key/value/note grid.</summary>
    public MetricTableView()
    {
        AutoGenerateColumns = false;
        IsReadOnly = true;
        CanUserReorderColumns = true;
        CanUserResizeColumns = true;
        CanUserSortColumns = true;
        GridLinesVisibility = DataGridGridLinesVisibility.Horizontal;
        HeadersVisibility = DataGridHeadersVisibility.Column;
        SelectionMode = DataGridSelectionMode.Single;
        Columns.Add(TextColumn("Key", nameof(MetricRow.Key), 140));
        Columns.Add(TextColumn("Value", nameof(MetricRow.Value), 120));
        Columns.Add(TextColumn("Note", nameof(MetricRow.Note)));
    }

    /// <summary>Binds metric rows.</summary>
    public void SetRows(IEnumerable<MetricRow> rows) => ItemsSource = rows.ToList();

    /// <summary>Creates a read-only text column.</summary>
    public static DataGridTextColumn TextColumn(string header, string bindingPath, double width = double.NaN)
    {
        var column = new DataGridTextColumn
        {
            Header = header,
            Binding = new global::Avalonia.Data.Binding(bindingPath),
            IsReadOnly = true,
        };
        if (!double.IsNaN(width))
            column.Width = new DataGridLength(width);
        return column;
    }
}
