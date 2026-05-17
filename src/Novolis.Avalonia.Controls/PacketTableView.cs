using Avalonia.Controls;

namespace Novolis.Avalonia.Controls;

/// <summary>Data grid tuned for high-volume packet or log rows.</summary>
public sealed class PacketTableView : DataGrid
{
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

    public void SetColumns(IEnumerable<DataGridColumn> columns)
    {
        Columns.Clear();
        foreach (var column in columns)
            Columns.Add(column);
    }

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
