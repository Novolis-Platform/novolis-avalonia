namespace Novolis.Avalonia.Cad.Session;

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
