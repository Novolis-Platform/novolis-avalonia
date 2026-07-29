namespace Novolis.Agent.Surface;

public interface IAgentSession
{
    AgentHelloDto Hello();
    AgentSnapshotDto Snapshot();
    AgentActionsResponseDto Actions();
    AgentCommandResultDto Execute(AgentCommandDto command);
    void Subscribe();
    event Action<AgentChangedEventDto>? Changed;
    event Action<AgentActionResultEventDto>? ActionResult;
}

public sealed class AgentHelloDto
{
    public string SurfaceId { get; set; } = "";
    public string ProtocolVersion { get; set; } = "1";
    public string AppId { get; set; } = "";
    public int? HttpPort { get; set; }
    public int? TcpPort { get; set; }
    public string[] Capabilities { get; set; } = [];
    public string? Description { get; set; }
}

public sealed class AgentSnapshotDto
{
    public string DocumentName { get; set; } = "";
    public int NodeCount { get; set; }
    public string? SelectionId { get; set; }
    public string? ActiveCameraId { get; set; }
    public string? LastAction { get; set; }
    public object? Document { get; set; }
    public IReadOnlyList<AgentActionDto> Actions { get; set; } = [];
}

public sealed class AgentActionDto
{
    public string Id { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Params { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public string? DisabledReason { get; set; }
    public Dictionary<string, object?>? Schema { get; set; }
}

public sealed class AgentActionsResponseDto
{
    public IReadOnlyList<AgentActionDto> Actions { get; set; } = [];
}

public sealed class AgentCommandDto
{
    public string ActionId { get; set; } = "";
    public string? Path { get; set; }
    public string? NodeId { get; set; }
    public string? ParentId { get; set; }
    public string? LightKind { get; set; }
    public string? Name { get; set; }
    public float? Intensity { get; set; }
    public float? X { get; set; }
    public float? Y { get; set; }
    public float? Z { get; set; }
    public float? Rx { get; set; }
    public float? Ry { get; set; }
    public float? Rz { get; set; }
    public string? GeneratorKind { get; set; }
    public string? ModifierKind { get; set; }
    public string? SourceId { get; set; }
    public string? InputId { get; set; }
    public int? Count { get; set; }
    public string? Axis { get; set; }
    public string? MaterialColor { get; set; }
    public Dictionary<string, object?>? Extra { get; set; }
}

public sealed class AgentCommandResultDto
{
    public bool Ok { get; set; }
    public string ActionId { get; set; } = "";
    public string Message { get; set; } = "";
    public string? ErrorCode { get; set; }
    public string? NodeId { get; set; }
}

public sealed class AgentChangedEventDto
{
    public string Reason { get; set; } = "changed";
    public string? DocumentName { get; set; }
    public int NodeCount { get; set; }
}

public sealed class AgentActionResultEventDto
{
    public bool Ok { get; set; }
    public string ActionId { get; set; } = "";
    public string Message { get; set; } = "";
}

public sealed class AgentSubscribeResponseDto
{
    public bool Ok { get; set; } = true;
}

public static class AgentMethodNames
{
    public const string Hello = "session.hello";
    public const string Snapshot = "session.snapshot";
    public const string Actions = "session.actions";
    public const string Command = "session.command";
    public const string Subscribe = "session.subscribe";
    public const string Changed = "session.changed";
    public const string ActionResult = "session.actionResult";
}
