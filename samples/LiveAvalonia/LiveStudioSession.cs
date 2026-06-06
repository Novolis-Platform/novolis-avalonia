using Novolis.Audio.Live;
using Novolis.Audio.Live.Protocol;
using Novolis.Audio.Live.Protocol.Dto;
using Novolis.Audio.Live.Repl;
using Novolis.Audio.Live.Visuals;

namespace LiveAvalonia;

internal sealed class LiveStudioSession : IAsyncDisposable
{
    private readonly LiveHostProcess _host = new();
    private readonly LiveReplClient _client = new();
    private readonly SemaphoreSlim _clientGate = new(1, 1);
    private readonly CancellationTokenSource _shutdown = new();
    private readonly object _graphGate = new();
    private LiveGraphNode? _graph;
    private Task? _pollingTask;
    private Task? _showcaseTask;
    private bool _started;

    public event Action<LiveTransportSnapshotDto?, LiveGraphNode?>? StateChanged;

    public async Task StartAsync()
    {
        if (_started)
            return;

        _started = true;

        await _host.StartAsync(_shutdown.Token).ConfigureAwait(false);
        await _client.ConnectAsync(LiveTransportEndpoints.CreateDefault(), _shutdown.Token).ConfigureAwait(false);

        await CompileShowcaseProgramAsync(version: 1, SwapPolicy.Immediately).ConfigureAwait(false);

        _pollingTask = PollSnapshotsAsync(_shutdown.Token);
        _showcaseTask = RunSecondProgramDemoAsync(_shutdown.Token);
    }

    public async ValueTask DisposeAsync()
    {
        _shutdown.Cancel();

        if (_showcaseTask is not null)
        {
            try
            {
                await _showcaseTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        if (_pollingTask is not null)
        {
            try
            {
                await _pollingTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        await _client.DisposeAsync().ConfigureAwait(false);
        await _host.DisposeAsync().ConfigureAwait(false);
        _clientGate.Dispose();
        _shutdown.Dispose();
    }

    private async Task RunSecondProgramDemoAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
            await CompileShowcaseProgramAsync(version: 2, SwapPolicy.NextBeat, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task CompileShowcaseProgramAsync(int version, SwapPolicy swapPolicy, CancellationToken cancellationToken = default)
    {
        var definition = LiveSamplePrograms.CreateProgram(version);
        var response = await SendAsync(
            token => _client.CompileAsync(definition, swapPolicy, token),
            cancellationToken).ConfigureAwait(false);

        if (response.Success && response.Program is not null)
        {
            lock (_graphGate)
            {
                _graph = LiveVisualProjection.FromProgram(response.Program.ToDomain());
            }
        }

        await PublishSnapshotAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task PollSnapshotsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));

            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                await PublishSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task PublishSnapshotAsync(CancellationToken cancellationToken)
    {
        var snapshot = await SendAsync(token => _client.SnapshotAsync(token), cancellationToken).ConfigureAwait(false);
        LiveGraphNode? graph;

        lock (_graphGate)
        {
            graph = _graph;
        }

        StateChanged?.Invoke(snapshot, graph);
    }

    private async ValueTask<T> SendAsync<T>(Func<CancellationToken, ValueTask<T>> action, CancellationToken cancellationToken)
    {
        await _clientGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await action(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _clientGate.Release();
        }
    }
}
