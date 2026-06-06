using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Novolis.Audio.Live.Protocol.Dto;
using Novolis.Audio.Live.Visuals;

namespace Novolis.Avalonia.Live;

public sealed class LiveStudioPanel : Grid
{
    private readonly LiveTransportStatusPanel _status = new();
    private readonly LiveProgramGraphView _graph = new();

    public LiveStudioPanel()
    {
        RowDefinitions = new RowDefinitions("Auto,*");
        ColumnDefinitions = new ColumnDefinitions("2*,3*");

        var statusBorder = new Border
        {
            Child = _status,
            Margin = new Thickness(8),
        };
        Grid.SetRow(statusBorder, 0);
        Grid.SetColumnSpan(statusBorder, 2);
        Children.Add(statusBorder);

        var graphBorder = new Border
        {
            Child = _graph,
            Margin = new Thickness(8),
        };
        Grid.SetRow(graphBorder, 1);
        Grid.SetColumn(graphBorder, 0);
        Grid.SetColumnSpan(graphBorder, 2);
        Children.Add(graphBorder);
    }

    public void Bind(LiveTransportSnapshotDto? snapshot, LiveGraphNode? graph)
    {
        _status.Bind(snapshot);
        _graph.Bind(graph);
    }
}
