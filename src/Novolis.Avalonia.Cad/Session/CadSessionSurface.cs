namespace Novolis.Avalonia.Cad.Session;

/// <summary>Attach HTTP + TCP CAD session transports (Sins <c>SessionSurface</c> pattern).</summary>
public sealed class CadSessionSurface : IAsyncDisposable
{
    private readonly List<IAsyncDisposable> _hosts = new();

    private CadSessionSurface()
    {
    }

    public CadSessionHttpHost? Http { get; private set; }

    public CadSessionTcpJsonlHost? Tcp { get; private set; }

    public string? HttpBaseUrl => Http?.BaseUrl;

    public int? TcpPort => Tcp?.Port;

    public static CadSessionSurface? TryAttachFromEnvironment(ICadSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var surface = new CadSessionSurface();
        var any = false;

        if (CadSessionEndpoints.IsHttpEnabledByEnvironment())
        {
            var http = CadSessionHttpHost.Attach(session);
            surface.Http = http;
            surface._hosts.Add(http);
            any = true;
        }

        if (CadSessionEndpoints.IsTcpEnabledByEnvironment())
        {
            var tcp = CadSessionTcpJsonlHost.Attach(session);
            surface.Tcp = tcp;
            surface._hosts.Add(tcp);
            any = true;
        }

        return any ? surface : null;
    }

    /// <summary>Attach HTTP and TCP unconditionally (bind failures ignored per transport).</summary>
    public static CadSessionSurface? AttachAll(ICadSession session, int? httpPort = null, int? tcpPort = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        var surface = new CadSessionSurface();
        var any = false;

        try
        {
            var http = CadSessionHttpHost.Attach(session, httpPort);
            surface.Http = http;
            surface._hosts.Add(http);
            any = true;
        }
        catch
        {
        }

        try
        {
            var tcp = CadSessionTcpJsonlHost.Attach(session, tcpPort);
            surface.Tcp = tcp;
            surface._hosts.Add(tcp);
            any = true;
        }
        catch
        {
        }

        return any ? surface : null;
    }

    public async ValueTask DisposeAsync()
    {
        for (var i = _hosts.Count - 1; i >= 0; i--)
        {
            try { await _hosts[i].DisposeAsync().ConfigureAwait(false); }
            catch { /* ignore */ }
        }

        _hosts.Clear();
    }
}
