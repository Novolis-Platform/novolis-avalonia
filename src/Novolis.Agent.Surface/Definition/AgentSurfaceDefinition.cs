using System.Reflection;
using System.Text.Json;

namespace Novolis.Agent.Surface;

/// <summary>Auto-constructed surface metadata from <see cref="AgentSurfaceAttribute"/> / <see cref="AgentActionAttribute"/>.</summary>
public sealed class AgentSurfaceDefinition
{
    private AgentSurfaceDefinition(
        string surfaceId,
        string protocolVersion,
        string enableEnv,
        string markerPrefix,
        int httpPort,
        int tcpPort,
        string? description,
        IReadOnlyList<AgentActionDto> actions,
        IReadOnlyList<string> methods)
    {
        SurfaceId = surfaceId;
        ProtocolVersion = protocolVersion;
        EnableEnv = enableEnv;
        MarkerPrefix = markerPrefix;
        DefaultHttpPort = httpPort;
        DefaultTcpPort = tcpPort;
        Description = description;
        Actions = actions;
        Methods = methods;
    }

    public string SurfaceId { get; }
    public string ProtocolVersion { get; }
    public string EnableEnv { get; }
    public string MarkerPrefix { get; }
    public int DefaultHttpPort { get; }
    public int DefaultTcpPort { get; }
    public string? Description { get; }
    public IReadOnlyList<AgentActionDto> Actions { get; }
    public IReadOnlyList<string> Methods { get; }

    public string HttpEnableEnv => EnableEnv + "_HTTP";
    public string HttpPortEnv => EnableEnv + "_HTTP_PORT";
    public string TcpEnableEnv => EnableEnv + "_TCP";
    public string TcpPortEnv => EnableEnv + "_TCP_PORT";
    public string HttpMarkerFileName => MarkerPrefix + ".http";
    public string TcpMarkerFileName => MarkerPrefix + ".tcp";
    public string HttpMarkerPath => Path.Combine(Path.GetTempPath(), HttpMarkerFileName);
    public string TcpMarkerPath => Path.Combine(Path.GetTempPath(), TcpMarkerFileName);

    public static AgentSurfaceDefinition From<T>() => From(typeof(T));

    public static AgentSurfaceDefinition From(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var surface = type.GetCustomAttribute<AgentSurfaceAttribute>()
                      ?? throw new InvalidOperationException($"{type.Name} requires [AgentSurface].");

        var actions = type.GetCustomAttributes<AgentActionAttribute>(inherit: true)
            .Select(a => new AgentActionDto
            {
                Id = a.ActionId,
                Summary = a.Summary,
                Params = a.Params,
                Enabled = a.EnabledByDefault,
                Schema = BuildParamSchema(a.Params),
            })
            .GroupBy(a => a.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(a => a.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
            .Select(m => m.GetCustomAttribute<AgentMethodAttribute>()?.Method)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .DefaultIfEmpty()
            .ToList();

        if (methods.Count == 0 || (methods.Count == 1 && methods[0] is null))
        {
            methods =
            [
                AgentMethodNames.Hello,
                AgentMethodNames.Snapshot,
                AgentMethodNames.Actions,
                AgentMethodNames.Command,
                AgentMethodNames.Subscribe,
            ];
        }

        return new AgentSurfaceDefinition(
            surface.SurfaceId,
            surface.ProtocolVersion,
            surface.EnableEnv,
            surface.MarkerPrefix,
            surface.HttpPort,
            surface.TcpPort,
            surface.Description,
            actions,
            methods!);
    }

    public AgentHelloDto BuildHello(string appId = "", int? httpPort = null, int? tcpPort = null) => new()
    {
        SurfaceId = SurfaceId,
        ProtocolVersion = ProtocolVersion,
        AppId = appId,
        HttpPort = httpPort ?? DefaultHttpPort,
        TcpPort = tcpPort ?? DefaultTcpPort,
        Capabilities = Methods.ToArray(),
        Description = Description,
    };

    public AgentActionsResponseDto BuildActions(Func<AgentActionDto, AgentActionDto>? policy = null)
    {
        var list = Actions.Select(a =>
        {
            var copy = new AgentActionDto
            {
                Id = a.Id,
                Summary = a.Summary,
                Params = a.Params,
                Enabled = a.Enabled,
                DisabledReason = a.DisabledReason,
                Schema = a.Schema,
            };
            return policy?.Invoke(copy) ?? copy;
        }).ToList();
        return new AgentActionsResponseDto { Actions = list };
    }

    public Dictionary<string, object?> BuildCommandJsonSchema() => new()
    {
        ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
        ["title"] = $"{SurfaceId}.command",
        ["type"] = "object",
        ["required"] = new[] { "actionId" },
        ["properties"] = new Dictionary<string, object?>
        {
            ["actionId"] = new Dictionary<string, object?>
            {
                ["type"] = "string",
                ["enum"] = Actions.Select(a => a.Id).ToArray(),
            },
            ["path"] = new Dictionary<string, object?> { ["type"] = "string" },
            ["nodeId"] = new Dictionary<string, object?> { ["type"] = "string", ["format"] = "uuid" },
            ["parentId"] = new Dictionary<string, object?> { ["type"] = "string", ["format"] = "uuid" },
            ["lightKind"] = new Dictionary<string, object?>
            {
                ["type"] = "string",
                ["enum"] = new[] { "omni", "spot", "infinite", "area" },
            },
            ["name"] = new Dictionary<string, object?> { ["type"] = "string" },
            ["intensity"] = new Dictionary<string, object?> { ["type"] = "number" },
            ["x"] = new Dictionary<string, object?> { ["type"] = "number" },
            ["y"] = new Dictionary<string, object?> { ["type"] = "number" },
            ["z"] = new Dictionary<string, object?> { ["type"] = "number" },
        },
    };

    public Dictionary<string, object?> BuildOpenApiFragment() => new()
    {
        ["openapi"] = "3.0.3",
        ["info"] = new Dictionary<string, object?>
        {
            ["title"] = $"Novolis Agent Surface ({SurfaceId})",
            ["version"] = ProtocolVersion,
            ["description"] = Description ?? $"Loopback agent surface '{SurfaceId}'.",
        },
        ["servers"] = new object[]
        {
            new Dictionary<string, object?> { ["url"] = $"http://127.0.0.1:{DefaultHttpPort}" },
        },
        ["paths"] = new Dictionary<string, object?>
        {
            ["/session/hello"] = GetPath("Hello"),
            ["/session/snapshot"] = GetPath("Snapshot"),
            ["/session/actions"] = GetPath("Actions catalog"),
            ["/session/command"] = PostPath("Execute action"),
            ["/session/subscribe"] = PostPath("Subscribe to events"),
            ["/session/events"] = GetPath("SSE event stream"),
            ["/health"] = GetPath("Health"),
        },
    };

    public IReadOnlyList<McpToolDescriptor> BuildMcpTools(string toolPrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolPrefix);
        var tools = new List<McpToolDescriptor>
        {
            new($"{toolPrefix}_hello", $"session.hello for {SurfaceId}", new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object?>(),
            }),
            new($"{toolPrefix}_snapshot", $"session.snapshot for {SurfaceId}", new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object?>(),
            }),
            new($"{toolPrefix}_actions", $"session.actions for {SurfaceId}", new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object?>(),
            }),
            new($"{toolPrefix}_command", $"session.command for {SurfaceId}", BuildCommandJsonSchema()),
        };
        return tools;
    }

    public string ToDiscoveryJson() => JsonSerializer.Serialize(new
    {
        surfaceId = SurfaceId,
        protocolVersion = ProtocolVersion,
        enableEnv = EnableEnv,
        httpPort = DefaultHttpPort,
        tcpPort = DefaultTcpPort,
        httpMarker = HttpMarkerPath,
        tcpMarker = TcpMarkerPath,
        actions = Actions,
        openApi = BuildOpenApiFragment(),
        commandSchema = BuildCommandJsonSchema(),
        mcpTools = BuildMcpTools(SurfaceId),
    }, AgentJson.Options);

    public bool IsEnabledByEnvironment() => EnvTruthy(Environment.GetEnvironmentVariable(EnableEnv));

    public bool IsHttpEnabledByEnvironment()
    {
        var http = Environment.GetEnvironmentVariable(HttpEnableEnv);
        if (EnvFalsy(http))
            return false;
        if (EnvTruthy(http))
            return true;
        return IsEnabledByEnvironment();
    }

    public bool IsTcpEnabledByEnvironment() => EnvTruthy(Environment.GetEnvironmentVariable(TcpEnableEnv));

    public int ResolveHttpPort()
    {
        var raw = Environment.GetEnvironmentVariable(HttpPortEnv);
        return int.TryParse(raw, out var port) && port is > 0 and < 65536 ? port : DefaultHttpPort;
    }

    public int ResolveTcpPort()
    {
        var raw = Environment.GetEnvironmentVariable(TcpPortEnv);
        return int.TryParse(raw, out var port) && port is > 0 and < 65536 ? port : DefaultTcpPort;
    }

    public string? TryReadHttpBaseUrl()
    {
        try
        {
            if (!File.Exists(HttpMarkerPath))
                return null;
            var lines = File.ReadAllLines(HttpMarkerPath);
            return lines.Length >= 2 ? lines[1].Trim() : null;
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, object?> GetPath(string summary) => new()
    {
        ["get"] = new Dictionary<string, object?>
        {
            ["summary"] = summary,
            ["responses"] = new Dictionary<string, object?>
            {
                ["200"] = new Dictionary<string, object?> { ["description"] = "ok" },
            },
        },
    };

    private static Dictionary<string, object?> PostPath(string summary) => new()
    {
        ["post"] = new Dictionary<string, object?>
        {
            ["summary"] = summary,
            ["responses"] = new Dictionary<string, object?>
            {
                ["200"] = new Dictionary<string, object?> { ["description"] = "ok" },
            },
        },
    };

    private static Dictionary<string, object?>? BuildParamSchema(string hint)
    {
        if (string.IsNullOrWhiteSpace(hint))
            return null;
        var props = new Dictionary<string, object?>();
        foreach (var part in hint.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var optional = part.EndsWith('?');
            var body = optional ? part[..^1] : part;
            var bits = body.Split('|', 2);
            var name = bits[0].Trim();
            if (bits.Length == 2)
            {
                props[name] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                    ["enum"] = bits[1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                };
            }
            else
            {
                props[name] = new Dictionary<string, object?> { ["type"] = "string" };
            }

            _ = optional;
        }

        return new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = props,
        };
    }

    private static bool EnvTruthy(string? value) =>
        string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);

    private static bool EnvFalsy(string? value) =>
        string.Equals(value, "0", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "no", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "off", StringComparison.OrdinalIgnoreCase);
}

public sealed record McpToolDescriptor(string Name, string Description, Dictionary<string, object?> InputSchema);

public static class AgentJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };
}
