using Avalonia.Controls;

namespace Novolis.Avalonia.Controls;

/// <summary>Data grid tuned for high-volume packet or log rows.</summary>
public sealed class PacketTableView : DataGrid
{
    /// <summary>Creates a read-only, sortable packet table with standard grid chrome.</summary>
    public PacketTableView()
    {
        AutoGenerateColumns = false;
        IsReadOnly = true;
        CanUserReorderColumns = true;
        CanUserResizeColumns = true;
        CanUserSortColumns = true;
        GridLinesVisibility = DataGridGridLinesVisibility.All;
        HeadersVisibility = DataGridHeadersVisibility.Column;
        SelectionMode = DataGridSelectionMode.Single;
    }

    /// <summary>Replaces all columns with the supplied definitions.</summary>
    /// <param name="columns">Columns to display.</param>
    public void SetColumns(IEnumerable<DataGridColumn> columns)
    {
        Columns.Clear();
        foreach (var column in columns)
            Columns.Add(column);
    }

    /// <summary>Creates a read-only text column bound to a property path.</summary>
    /// <param name="header">Column header text.</param>
    /// <param name="bindingPath">Binding path on the row item.</param>
    /// <param name="width">Optional fixed width; <see cref="double.NaN"/> for auto.</param>
    /// <returns>Configured text column.</returns>
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
