namespace Novolis.Avalonia.Cad.Session;

public sealed class CadActionDto
{
    public string Id { get; set; } = "";

    public string Label { get; set; } = "";

    public bool Enabled { get; set; }

    public string? DisabledReason { get; set; }
}
