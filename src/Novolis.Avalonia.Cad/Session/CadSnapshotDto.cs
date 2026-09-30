namespace Novolis.Avalonia.Cad.Session;

public sealed class CadSnapshotDto
{
    public string DocumentName { get; set; } = "";

    public string DocumentPath { get; set; } = "";

    public bool Dirty { get; set; }

    public int EntityCount { get; set; }

    public Guid? SelectedId { get; set; }

    public Guid[] SelectedIds { get; set; } = [];

    public string ActiveTool { get; set; } = "select";

    public string ViewMode { get; set; } = "cad";

    public string Workspace { get; set; } = "cad";

    public string SelectionMode { get; set; } = "object";

    public float DrawElevation { get; set; }

    public string DisplayUnit { get; set; } = "meter";

    public bool SnapToGrid { get; set; }

    public float GridStep { get; set; }

    public string AxisLock { get; set; } = "none";

    public CadLastActionDto? LastAction { get; set; }

    public string[] RecentExportPaths { get; set; } = [];

    public CadActionDto[] Actions { get; set; } = [];
}
