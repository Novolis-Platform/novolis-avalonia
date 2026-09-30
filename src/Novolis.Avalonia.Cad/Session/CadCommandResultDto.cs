namespace Novolis.Avalonia.Cad.Session;

public sealed class CadCommandResultDto
{
    public bool Ok { get; set; }

    public string ActionId { get; set; } = "";

    public string Message { get; set; } = "";

    public string? ErrorCode { get; set; }

    public CadSnapshotDto? Snapshot { get; set; }

    public string[]? Paths { get; set; }
}
