namespace Novolis.Avalonia.Cad.Session;

public sealed class CadChangedEventDto
{
    public string Reason { get; set; } = "";

    public CadSnapshotDto? Snapshot { get; set; }
}
