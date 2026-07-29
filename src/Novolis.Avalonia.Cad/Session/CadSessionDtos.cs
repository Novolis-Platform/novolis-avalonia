namespace Novolis.Avalonia.Cad.Session;

public interface ICadSession
{
    CadHelloResponseDto Hello();

    CadSnapshotDto Snapshot();

    CadActionsResponseDto Actions();

    CadCommandResultDto Execute(CadCommandDto command);

    void Subscribe();

    event Action<CadChangedEventDto>? Changed;

    event Action<CadActionResultEventDto>? ActionResult;
}

public interface ICadSessionTransport
{
    string Kind { get; }

    ValueTask StartAsync(CancellationToken cancellationToken = default);

    ValueTask StopAsync(CancellationToken cancellationToken = default);
}

public sealed class CadHelloResponseDto
{
    public string ProtocolVersion { get; set; } = "1.0";

    public string AppId { get; set; } = "novolis.cad";

    public string AppTitle { get; set; } = "Novolis CAD";

    public int ProcessId { get; set; } = Environment.ProcessId;

    public string[] Capabilities { get; set; } =
    [
        "snapshot", "actions", "command", "export", "events",
    ];
}

public sealed class CadCommandDto
{
    public string ActionId { get; set; } = "";

    public string? Path { get; set; }

    public string? Tool { get; set; }

    public string? ViewMode { get; set; }

    public string? Prompt { get; set; }

    public Guid? EntityId { get; set; }

    public float? Elevation { get; set; }

    public float? GridStep { get; set; }

    public bool? Snap { get; set; }

    public string? Kind { get; set; }

    public string? ExportRoot { get; set; }

    public Dictionary<string, string>? Properties { get; set; }
}

public sealed class CadCommandResultDto
{
    public bool Ok { get; set; }

    public string ActionId { get; set; } = "";

    public string Message { get; set; } = "";

    public string? ErrorCode { get; set; }

    public CadSnapshotDto? Snapshot { get; set; }

    public string[]? Paths { get; set; }
}

public sealed class CadActionDto
{
    public string Id { get; set; } = "";

    public string Label { get; set; } = "";

    public bool Enabled { get; set; }

    public string? DisabledReason { get; set; }
}

public sealed class CadActionsResponseDto
{
    public CadActionDto[] Actions { get; set; } = [];
}

public sealed class CadLastActionDto
{
    public string ActionId { get; set; } = "";

    public bool Ok { get; set; }

    public string Message { get; set; } = "";

    public string? ErrorCode { get; set; }
}

public sealed class CadSnapshotDto
{
    public string DocumentName { get; set; } = "";

    public string DocumentPath { get; set; } = "";

    public bool Dirty { get; set; }

    public int EntityCount { get; set; }

    public Guid? SelectedId { get; set; }

    public string ActiveTool { get; set; } = "select";

    public string ViewMode { get; set; } = "draft";

    public float DrawElevation { get; set; }

    public string DisplayUnit { get; set; } = "meter";

    public bool SnapToGrid { get; set; }

    public float GridStep { get; set; }

    public CadLastActionDto? LastAction { get; set; }

    public string[] RecentExportPaths { get; set; } = [];

    public CadActionDto[] Actions { get; set; } = [];
}

public sealed class CadChangedEventDto
{
    public string Reason { get; set; } = "";

    public CadSnapshotDto? Snapshot { get; set; }
}

public sealed class CadActionResultEventDto
{
    public string ActionId { get; set; } = "";

    public bool Ok { get; set; }

    public string Message { get; set; } = "";

    public string? ErrorCode { get; set; }

    public CadSnapshotDto? Snapshot { get; set; }
}

public sealed class CadSubscribeResponseDto
{
    public bool Ok { get; set; } = true;
}
