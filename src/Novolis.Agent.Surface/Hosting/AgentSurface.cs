namespace Novolis.Agent.Surface;

/// <summary>Attach HTTP + TCP agent transports from a <see cref="AgentSurfaceDefinition"/>.</summary>
public sealed class AgentSurface : IAsyncDisposable
{
    private readonly List<IAsyncDisposable> _hosts = new();

    private AgentSurface()
    {
    }

    public AgentHttpHost? Http { get; private set; }
    public AgentTcpJsonlHost? Tcp { get; private set; }
    public AgentSurfaceDefinition? Definition { get; private set; }
    public string? HttpBaseUrl => Http?.BaseUrl;
    public int? TcpPort => Tcp?.Port;

    public static AgentSurface? TryAttachFromEnvironment(IAgentSession session, AgentSurfaceDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(definition);
        var surface = new AgentSurface { Definition = definition };
        var any = false;

        if (definition.IsHttpEnabledByEnvironment())
        {
            var http = AgentHttpHost.Attach(session, definition);
            surface.Http = http;
            surface._hosts.Add(http);
            any = true;
        }

        if (definition.IsTcpEnabledByEnvironment())
        {
            var tcp = AgentTcpJsonlHost.Attach(session, definition);
            surface.Tcp = tcp;
            surface._hosts.Add(tcp);
            any = true;
        }

        return any ? surface : null;
    }

    public static AgentSurface? AttachAll(
        IAgentSession session,
        AgentSurfaceDefinition definition,
        int? httpPort = null,
        int? tcpPort = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(definition);
        var surface = new AgentSurface { Definition = definition };
        var any = false;

        try
        {
            var http = AgentHttpHost.Attach(session, definition, httpPort);
            surface.Http = http;
            surface._hosts.Add(http);
            any = true;
        }
        catch { }

        try
        {
            var tcp = AgentTcpJsonlHost.Attach(session, definition, tcpPort);
            surface.Tcp = tcp;
            surface._hosts.Add(tcp);
            any = true;
        }
        catch { }

        return any ? surface : null;
    }

    public async ValueTask DisposeAsync()
    {
        for (var i = _hosts.Count - 1; i >= 0; i--)
        {
            try { await _hosts[i].DisposeAsync().ConfigureAwait(false); }
            catch { }
        }

        _hosts.Clear();
    }
}
