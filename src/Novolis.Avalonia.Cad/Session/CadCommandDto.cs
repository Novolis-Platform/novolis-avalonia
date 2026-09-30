namespace Novolis.Avalonia.Cad.Session;

public sealed class CadCommandDto
{
    public string ActionId { get; set; } = "";

    public string? Path { get; set; }

    public string? Tool { get; set; }

    public string? ViewMode { get; set; }

    public string? Workspace { get; set; }

    public string? SelectionMode { get; set; }

    public string? Prompt { get; set; }

    public Guid? EntityId { get; set; }

    public Guid? TargetId { get; set; }

    public Guid? CutterId { get; set; }

    public Guid? SourceId { get; set; }

    public Guid? PrototypeId { get; set; }

    public Guid[]? MemberIds { get; set; }

    public string? Operation { get; set; }

    public string? Mode { get; set; }

    public string? LinkMode { get; set; }

    public string? Realization { get; set; }

    public bool? MergeAtPlane { get; set; }

    public float? Tolerance { get; set; }

    public int[]? Counts { get; set; }

    public float[]? Spacing { get; set; }

    /// <summary>Radial clone axis (unit vector preferred).</summary>
    public float[]? Axis { get; set; }

    public float? StepRadians { get; set; }

    /// <summary>Instance / transform translation (meters).</summary>
    public float[]? Center { get; set; }

    public float? Elevation { get; set; }

    public float? GridStep { get; set; }

    public bool? Snap { get; set; }

    public string? Kind { get; set; }

    public string? ExportRoot { get; set; }

    public Dictionary<string, string>? Properties { get; set; }
}
