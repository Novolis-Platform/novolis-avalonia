using Avalonia.Controls;
using Avalonia.Threading;
using Novolis.Avalonia.Agent.Protocol;
using Novolis.Avalonia.Agent.Protocol.Dto;
using Novolis.Transports.LocalIpc;

namespace Novolis.Avalonia.Agent;

public sealed class AgentHost : IAsyncDisposable
{
    public const string EnableEnvVar = "NOVOLIS_AVALONIA_AGENT";

    private readonly Window _window;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _listenTask;
    private ILocalIpcListener? _listener;

    private AgentHost(Window window, LocalIpcEndpoint endpoint)
    {
        _window = window;
        _listenTask = Task.Run(() => ListenAsync(endpoint, _cts.Token));
    }

    public static AgentHost Attach(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        return new AgentHost(window, UiTransportEndpoints.CreateDefault());
    }

    public static AgentHost Attach(Window window, string endpointAddress)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointAddress);
        var endpoint = OperatingSystem.IsWindows()
            ? new LocalIpcEndpoint(endpointAddress, LocalIpcTransportKind.NamedPipe)
            : new LocalIpcEndpoint(endpointAddress, LocalIpcTransportKind.UnixDomainSocket);
        return new AgentHost(window, endpoint);
    }

    public static AgentHost? TryAttachFromEnvironment(Window window)
    {
        if (!IsEnabledByEnvironment())
            return null;
        return Attach(window);
    }

    public static bool IsEnabledByEnvironment()
    {
        var value = Environment.GetEnvironmentVariable(EnableEnvVar);
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private async Task ListenAsync(LocalIpcEndpoint endpoint, CancellationToken cancellationToken)
    {
        try
        {
            var marker = Path.Combine(Path.GetTempPath(), "novolis-avalonia-agent.host");
            await File.WriteAllTextAsync(marker, $"{Environment.ProcessId}\n{endpoint.Kind}\n{endpoint.Address}\n", cancellationToken)
                .ConfigureAwait(false);

            _listener = LocalIpcTransport.CreateListener(endpoint);
            while (!cancellationToken.IsCancellationRequested)
            {
                var connection = await _listener.AcceptAsync(cancellationToken).ConfigureAwait(false);
                _ = Task.Run(() => HandleConnectionAsync(connection, cancellationToken), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // shutting down
        }
        catch (Exception ex)
        {
            CrashGuard.ReportSilent(ex, "AgentHost.listen");
            try
            {
                await File.WriteAllTextAsync(
                    Path.Combine(Path.GetTempPath(), "novolis-avalonia-agent.host.error"),
                    ex.ToString(),
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // ignore secondary logging failures
            }
        }
    }

    private async Task HandleConnectionAsync(ILocalIpcConnection connection, CancellationToken cancellationToken)
    {
        await using (connection)
        {
            try
            {
                await foreach (var frame in connection.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (frame.Kind != UiRpcMessageKinds.Request)
                        continue;

                    try
                    {
                        await DispatchAsync(connection, frame, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        CrashGuard.ReportSilent(ex, $"AgentHost.{frame.Name}");
                        try
                        {
                            await ReplyFaultAsync(connection, frame, ex.Message).ConfigureAwait(false);
                        }
                        catch
                        {
                            // connection may already be dead
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // connection closed
            }
            catch (Exception ex)
            {
                CrashGuard.ReportSilent(ex, "AgentHost.connection");
                try
                {
                    await File.AppendAllTextAsync(
                        Path.Combine(Path.GetTempPath(), "novolis-avalonia-agent.host.error"),
                        ex + Environment.NewLine,
                        CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            }
        }
    }

    private async Task DispatchAsync(ILocalIpcConnection connection, LocalIpcFrame frame, CancellationToken cancellationToken)
    {
        switch (frame.Name)
        {
            case UiRpcMethodNames.Hello:
                await ReplyAsync(connection, frame, await OnUiAsync(() =>
                        HandleHello(UiProtocolCodec.Deserialize<UiHelloRequestDto>(frame.Payload)))
                    .ConfigureAwait(false)).ConfigureAwait(false);
                break;
            case UiRpcMethodNames.Tree:
                await ReplyAsync(connection, frame, await OnUiAsync(() =>
                        HandleTree(UiProtocolCodec.Deserialize<UiTreeRequestDto>(frame.Payload)))
                    .ConfigureAwait(false)).ConfigureAwait(false);
                break;
            case UiRpcMethodNames.Screenshot:
                await ReplyAsync(connection, frame, await OnUiAsync(() =>
                        HandleScreenshot(UiProtocolCodec.Deserialize<UiScreenshotRequestDto>(frame.Payload)))
                    .ConfigureAwait(false)).ConfigureAwait(false);
                break;
            case UiRpcMethodNames.Click:
                await ReplyAsync(connection, frame, await OnUiAsync(() =>
                        HandleClick(UiProtocolCodec.Deserialize<UiClickRequestDto>(frame.Payload)))
                    .ConfigureAwait(false)).ConfigureAwait(false);
                break;
            case UiRpcMethodNames.Type:
                await ReplyAsync(connection, frame, await OnUiAsync(() =>
                        HandleType(UiProtocolCodec.Deserialize<UiTypeRequestDto>(frame.Payload)))
                    .ConfigureAwait(false)).ConfigureAwait(false);
                break;
            case UiRpcMethodNames.Select:
                await ReplyAsync(connection, frame, await OnUiAsync(() =>
                        HandleSelect(UiProtocolCodec.Deserialize<UiSelectRequestDto>(frame.Payload)))
                    .ConfigureAwait(false)).ConfigureAwait(false);
                break;
            case UiRpcMethodNames.Wait:
                await ReplyAsync(connection, frame,
                        await HandleWaitAsync(UiProtocolCodec.Deserialize<UiWaitRequestDto>(frame.Payload), cancellationToken)
                            .ConfigureAwait(false))
                    .ConfigureAwait(false);
                break;
            case UiRpcMethodNames.Get:
                await ReplyAsync(connection, frame, await OnUiAsync(() =>
                        AgentQuery.Get(_window, UiProtocolCodec.Deserialize<UiGetRequestDto>(frame.Payload)))
                    .ConfigureAwait(false)).ConfigureAwait(false);
                break;
            case UiRpcMethodNames.Items:
                await ReplyAsync(connection, frame, await OnUiAsync(() =>
                        AgentQuery.Items(_window, UiProtocolCodec.Deserialize<UiItemsRequestDto>(frame.Payload)))
                    .ConfigureAwait(false)).ConfigureAwait(false);
                break;
        }
    }

    private static async Task ReplyFaultAsync(ILocalIpcConnection connection, LocalIpcFrame frame, string error)
    {
        // Best-effort typed fault so clients do not hang waiting for a response body.
        object payload = frame.Name switch
        {
            UiRpcMethodNames.Hello => new UiHelloResponseDto(0, false, error, UiProtocolVersion.Current, null, 0),
            UiRpcMethodNames.Tree => new UiTreeResponseDto(0, false, error, Array.Empty<UiTreeNodeDto>()),
            UiRpcMethodNames.Screenshot => new UiScreenshotResponseDto(0, false, error, null, 0, 0),
            UiRpcMethodNames.Click => new UiClickResponseDto(0, false, error, null),
            UiRpcMethodNames.Type => new UiTypeResponseDto(0, false, error),
            UiRpcMethodNames.Select => new UiSelectResponseDto(0, false, error, null, null),
            UiRpcMethodNames.Wait => new UiWaitResponseDto(0, false, error, true),
            UiRpcMethodNames.Get => new UiGetResponseDto(0, false, error, Array.Empty<UiControlStateDto>(), null, 0),
            UiRpcMethodNames.Items => new UiItemsResponseDto(0, false, error, "", null, null, Array.Empty<UiItemDto>()),
            _ => new UiHelloResponseDto(0, false, error, UiProtocolVersion.Current, null, 0),
        };
        await ReplyAsync(connection, frame, payload).ConfigureAwait(false);
    }

    private UiHelloResponseDto HandleHello(UiHelloRequestDto request) =>
        new(request.RequestId, true, null, UiProtocolVersion.Current, _window.Title, Environment.ProcessId);

    private UiTreeResponseDto HandleTree(UiTreeRequestDto request)
    {
        var nodes = AgentTreeWalker.Collect(_window, request.InteractiveOnly);
        return new UiTreeResponseDto(request.RequestId, true, null, nodes);
    }

    private UiScreenshotResponseDto HandleScreenshot(UiScreenshotRequestDto request) =>
        AgentScreenshot.Capture(_window, request.ControlId, request.MaxWidth, request.RequestId);

    private UiClickResponseDto HandleClick(UiClickRequestDto request) =>
        AgentInput.Click(_window, request);

    private UiTypeResponseDto HandleType(UiTypeRequestDto request) =>
        AgentInput.Type(_window, request);

    private UiSelectResponseDto HandleSelect(UiSelectRequestDto request) =>
        AgentInput.Select(_window, request);

    private async Task<UiWaitResponseDto> HandleWaitAsync(UiWaitRequestDto request, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromMilliseconds(Math.Max(0, request.TimeoutMs));
        var deadline = DateTime.UtcNow + timeout;

        while (true)
        {
            var matched = await OnUiAsync(() =>
            {
                var control = AgentTreeWalker.FindById(_window, request.ControlId);
                if (control is null)
                    return false;
                if (request.Enabled is bool enabled && control.IsEnabled != enabled)
                    return false;
                if (request.TextContains is { Length: > 0 } text)
                {
                    var value = control switch
                    {
                        TextBox tb => tb.Text,
                        TextBlock tb => tb.Text,
                        ContentControl { Content: string s } => s,
                        ContentControl cc => cc.Content?.ToString(),
                        _ => AgentProperties.GetId(control)
                    };
                    if (value is null || !value.Contains(text, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                return true;
            }).ConfigureAwait(false);

            if (matched)
                return new UiWaitResponseDto(request.RequestId, true, null, false);

            if (DateTime.UtcNow >= deadline)
                return new UiWaitResponseDto(request.RequestId, false, $"Timed out waiting for '{request.ControlId}'.", true);

            try
            {
                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return new UiWaitResponseDto(request.RequestId, false, "Cancelled.", true);
            }
        }
    }

    private static async Task ReplyAsync<T>(ILocalIpcConnection connection, LocalIpcFrame frame, T payload)
    {
        await connection.SendMessageAsync(frame.Sequence, UiRpcMessageKinds.Response, frame.Name, payload)
            .ConfigureAwait(false);
    }

    private static Task<T> OnUiAsync<T>(Func<T> action) =>
        Dispatcher.UIThread.InvokeAsync(action).GetTask();

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync().ConfigureAwait(false);
        try
        {
            await _listenTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        if (_listener is not null)
            await _listener.DisposeAsync().ConfigureAwait(false);
        _cts.Dispose();
    }
}
