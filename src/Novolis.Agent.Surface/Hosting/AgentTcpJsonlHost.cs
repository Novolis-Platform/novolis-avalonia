using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Novolis.Agent.Surface;

public sealed class AgentTcpJsonlHost : IAsyncDisposable
{
    private readonly IAgentSession _session;
    private readonly AgentSurfaceDefinition _definition;
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loop;
    private readonly object _eventGate = new();
    private readonly List<StreamWriter> _eventWriters = new();
    private readonly Action<AgentChangedEventDto> _onChanged;
    private readonly Action<AgentActionResultEventDto> _onActionResult;

    private AgentTcpJsonlHost(IAgentSession session, AgentSurfaceDefinition definition, int port)
    {
        _session = session;
        _definition = definition;
        Port = port;
        _listener = new TcpListener(IPAddress.Loopback, port);
        _onChanged = e => Broadcast(AgentMethodNames.Changed, e);
        _onActionResult = e => Broadcast(AgentMethodNames.ActionResult, e);
        _session.Changed += _onChanged;
        _session.ActionResult += _onActionResult;
        _listener.Start();
        _loop = Task.Run(() => ListenAsync(_cts.Token));
        try
        {
            File.WriteAllText(definition.TcpMarkerPath, $"{Environment.ProcessId}\n{port}\n");
        }
        catch { }
    }

    public string Kind => "tcp-jsonl";
    public int Port { get; }

    public static AgentTcpJsonlHost Attach(IAgentSession session, AgentSurfaceDefinition definition, int? port = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(definition);
        return new AgentTcpJsonlHost(session, definition, port ?? definition.ResolveTcpPort());
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            try
            {
                await File.WriteAllTextAsync(_definition.TcpMarkerPath + ".error", ex.ToString(), CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch { }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        await using (var stream = client.GetStream())
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                    if (line is null)
                        break;
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    object? id = null;
                    try
                    {
                        using var doc = JsonDocument.Parse(line);
                        var root = doc.RootElement;
                        id = root.TryGetProperty("id", out var idEl) ? idEl.Clone() : null;
                        var method = root.TryGetProperty("method", out var m) ? m.GetString() : null;
                        if (string.Equals(method, AgentMethodNames.Subscribe, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(method, "subscribe", StringComparison.OrdinalIgnoreCase))
                        {
                            lock (_eventGate)
                                _eventWriters.Add(writer);
                        }

                        var result = AgentJsonDispatcher.Dispatch(_session, method, root);
                        var reply = JsonSerializer.Serialize(new { id, ok = true, result }, AgentJson.Options);
                        await writer.WriteLineAsync(reply).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        var reply = JsonSerializer.Serialize(new { id, ok = false, error = ex.Message }, AgentJson.Options);
                        try { await writer.WriteLineAsync(reply).ConfigureAwait(false); } catch { }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch { }
            finally
            {
                lock (_eventGate)
                    _eventWriters.Remove(writer);
            }
        }
    }

    private void Broadcast(string eventName, object payload)
    {
        string line;
        try
        {
            line = JsonSerializer.Serialize(new { eventName, payload }, AgentJson.Options);
        }
        catch { return; }

        List<StreamWriter> writers;
        lock (_eventGate)
            writers = _eventWriters.ToList();

        foreach (var w in writers)
        {
            try { w.WriteLine(line); }
            catch
            {
                lock (_eventGate)
                    _eventWriters.Remove(w);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _session.Changed -= _onChanged;
        _session.ActionResult -= _onActionResult;
        await _cts.CancelAsync().ConfigureAwait(false);
        try { _listener.Stop(); } catch { }
        lock (_eventGate)
            _eventWriters.Clear();
        try { await _loop.ConfigureAwait(false); } catch { }
        _cts.Dispose();
        try { File.Delete(_definition.TcpMarkerPath); } catch { }
    }
}
