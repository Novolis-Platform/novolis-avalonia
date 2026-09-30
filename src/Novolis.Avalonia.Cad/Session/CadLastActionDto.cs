namespace Novolis.Avalonia.Cad.Session;

public sealed class CadLastActionDto
{
    public string ActionId { get; set; } = "";

    public bool Ok { get; set; }

    public string Message { get; set; } = "";

    public string? ErrorCode { get; set; }
}
