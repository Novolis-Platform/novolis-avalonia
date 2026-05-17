using Avalonia.Controls;
using Avalonia.Layout;

namespace Novolis.Avalonia.Layout;

/// <summary>
/// WireShark-style analyzer shell: toolbar, filter bar, packet list (top),
/// protocol tree and hex dump (bottom split).
/// </summary>
public sealed class AnalyzerWorkspace : Grid
{
    public AnalyzerWorkspace(Control packetList, Control protocolTree, Control hexDump)
    {
        RowDefinitions =
        [
            new RowDefinition(GridLength.Auto),
            new RowDefinition(GridLength.Auto),
            new RowDefinition(new GridLength(3, GridUnitType.Star)),
            new RowDefinition(GridLength.Auto),
            new RowDefinition(new GridLength(2, GridUnitType.Star)),
        ];

        Toolbar = new ToolbarRow();
        FilterBar = new FilterBar();
        PacketList = packetList;
        ProtocolTree = protocolTree;
        HexDump = hexDump;

        var bottomSplit = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
            ],
        };
        bottomSplit.Children.Add(ProtocolTree);
        Grid.SetColumn(ProtocolTree, 0);
        bottomSplit.Children.Add(new GridSplitter
        {
            Width = 5,
            ResizeDirection = GridResizeDirection.Columns,
        });
        Grid.SetColumn(bottomSplit.Children[^1], 1);
        bottomSplit.Children.Add(HexDump);
        Grid.SetColumn(HexDump, 2);

        Children.Add(Toolbar);
        Grid.SetRow(Toolbar, 0);
        Children.Add(FilterBar);
        Grid.SetRow(FilterBar, 1);
        Children.Add(PacketList);
        Grid.SetRow(PacketList, 2);
        Children.Add(new GridSplitter
        {
            Height = 5,
            ResizeDirection = GridResizeDirection.Rows,
        });
        Grid.SetRow(Children[^1], 3);
        Children.Add(bottomSplit);
        Grid.SetRow(bottomSplit, 4);
    }

    public ToolbarRow Toolbar { get; }

    public FilterBar FilterBar { get; }

    public Control PacketList { get; }

    public Control ProtocolTree { get; }

    public Control HexDump { get; }
}
