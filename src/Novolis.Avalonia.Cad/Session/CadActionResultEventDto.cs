namespace Novolis.Avalonia.Cad.Session;

public sealed class CadActionResultEventDto
{
    public string ActionId { get; set; } = "";

    public bool Ok { get; set; }

    public string Message { get; set; } = "";

    public string? ErrorCode { get; set; }

    public CadSnapshotDto? Snapshot { get; set; }
}
