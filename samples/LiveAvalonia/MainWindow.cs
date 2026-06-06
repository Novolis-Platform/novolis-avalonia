using Avalonia.Controls;
using Avalonia.Threading;
using Novolis.Audio.Live.Protocol.Dto;
using Novolis.Audio.Live.Visuals;
using Novolis.Avalonia.Live;

namespace LiveAvalonia;

internal sealed class MainWindow : Window
{
    private readonly LiveStudioPanel _panel = new();
    private readonly LiveStudioSession _session = new();

    public MainWindow()
    {
        Title = "Novolis Audio Live";
        Width = 1200;
        Height = 800;
        Content = _panel;

        _session.StateChanged += OnStateChanged;
        Opened += OnOpened;
        Closed += OnClosed;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        try
        {
            await _session.StartAsync();
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var snapshot = new LiveTransportSnapshotDto(
                    null,
                    null,
                    0m,
                    0m,
                    1,
                    1,
                    null,
                    null,
                    ex.Message);

                _panel.Bind(snapshot, null);
            });
        }
    }

    private async void OnClosed(object? sender, EventArgs e) => await _session.DisposeAsync();

    private void OnStateChanged(LiveTransportSnapshotDto? snapshot, LiveGraphNode? graph) =>
        Dispatcher.UIThread.Post(() => _panel.Bind(snapshot, graph));
}
