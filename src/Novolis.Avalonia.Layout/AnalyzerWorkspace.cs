using Avalonia.Controls;
using Avalonia.Layout;

namespace Novolis.Avalonia.Layout;

/// <summary>
/// WireShark-style analyzer shell: toolbar, filter bar, packet list (top),
/// protocol tree and hex dump (bottom split).
/// </summary>
public sealed class AnalyzerWorkspace : Grid
{
    /// <summary>
    /// Creates the workspace layout and wires the supplied list, tree, and hex panes.
    /// </summary>
    /// <param name="packetList">Primary packet or event list (top pane).</param>
    /// <param name="protocolTree">Structured detail tree (bottom left).</param>
    /// <param name="hexDump">Hex dump panel (bottom right).</param>
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

    /// <summary>Top toolbar row for capture controls and status.</summary>
    public ToolbarRow Toolbar { get; }

    /// <summary>Filter expression bar below the toolbar.</summary>
    public FilterBar FilterBar { get; }

    /// <summary>Packet or event list hosted in the upper main pane.</summary>
    public Control PacketList { get; }

    /// <summary>Protocol or object detail tree in the lower-left pane.</summary>
    public Control ProtocolTree { get; }

    /// <summary>Hex dump view in the lower-right pane.</summary>
    public Control HexDump { get; }
}
