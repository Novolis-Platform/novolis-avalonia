using System.Text.Json;

namespace Novolis.Avalonia.Cad.Core;

/// <summary>Mutable editor preferences (snap, grid, elevation, last document).</summary>
public sealed class CadEditorOptions
{
    public double LeftColumnPixels { get; set; } = 260;

    public double RightColumnPixels { get; set; } = 280;

    public bool SnapToGrid { get; set; } = true;

    public float GridStep { get; set; } = 0.5f;

    public string ViewMode { get; set; } = "draft";

    public string DisplayUnit { get; set; } = CadUnits.Meter;

    public string? LastDocumentPath { get; set; }

    public float DrawElevation { get; set; }

    public bool ContinuousLine { get; set; }

    public bool IsolateLevel { get; set; } = true;

    public float LevelTolerance { get; set; } = 0.05f;
}

/// <summary>
/// Workspace paths + persisted options. Host apps may point <see cref="DataRoot"/> at their LocalAppData folder.
/// </summary>
public sealed class CadEditorSettings
{
    private readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public CadEditorSettings(string? dataRoot = null, string workspaceFolderName = "default-workspace")
    {
        DataRoot = string.IsNullOrWhiteSpace(dataRoot)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Novolis",
                "Cad")
            : dataRoot;
        WorkspaceFolderName = workspaceFolderName;
    }

    public string DataRoot { get; }

    public string WorkspaceFolderName { get; }

    public string SettingsPath => Path.Combine(DataRoot, "settings.json");

    public string WorkspacePath => Path.Combine(DataRoot, WorkspaceFolderName);

    public string DocumentPath => Path.Combine(WorkspacePath, "draft.cadjson");

    public string PhysDocumentPath => Path.Combine(WorkspacePath, "draft.cadphys.json");

    /// <summary>Nested options (kept as <c>Settings</c> for call-site compatibility).</summary>
    public CadEditorOptions Settings { get; private set; } = new();

    public void Load()
    {
        Directory.CreateDirectory(DataRoot);
        if (!File.Exists(SettingsPath))
            return;

        var loaded = JsonSerializer.Deserialize<CadEditorOptions>(File.ReadAllText(SettingsPath), _json);
        if (loaded is not null)
            Settings = loaded;
    }

    public void Save()
    {
        Directory.CreateDirectory(DataRoot);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Settings, _json));
    }
}
