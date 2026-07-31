using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Novolis.Agent.Surface;
using Novolis.Avalonia._3D.Session;
using Novolis.Modeling.Import;

namespace Novolis.Avalonia._3D.Ui;

/// <summary>Desktop Open / Save / Save As / Import for scenes and Assimp meshes.</summary>
public static class SceneFileActions
{
    private static readonly FilePickerFileType SceneJsonType = new("Scene (.nov3djson)")
    {
        Patterns = ["*.nov3djson"],
        AppleUniformTypeIdentifiers = ["public.json"],
        MimeTypes = ["application/json", "application/octet-stream"],
    };

    private static readonly FilePickerFileType MeshImportType = new("Meshes (FBX, OBJ, glTF, …)")
    {
        Patterns = ["*.fbx", "*.obj", "*.gltf", "*.glb", "*.dae", "*.3ds", "*.blend", "*.stl", "*.ply"],
        AppleUniformTypeIdentifiers = ["public.data"],
        MimeTypes = ["application/octet-stream"],
    };

    public static void Open(Control host, SceneSessionService session, Action<string>? notice = null) =>
        _ = RunSafe(() => OpenAsync(host, session, notice), notice);

    public static void Save(Control host, SceneSessionService session, Action<string>? notice = null) =>
        _ = RunSafe(() => SaveAsync(host, session, notice), notice);

    public static void SaveAs(Control host, SceneSessionService session, Action<string>? notice = null) =>
        _ = RunSafe(() => SaveAsAsync(host, session, notice), notice);

    public static void ImportMesh(Control host, SceneSessionService session, Action<string>? notice = null, float? targetLengthMeters = null) =>
        _ = RunSafe(() => ImportMeshAsync(host, session, notice, targetLengthMeters), notice);

    public static async Task OpenAsync(Control host, SceneSessionService session, Action<string>? notice = null)
    {
        var sp = Storage(host);
        if (sp is null)
        {
            notice?.Invoke("Open unavailable (no window).");
            return;
        }

        var files = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Scene",
            AllowMultiple = false,
            FileTypeFilter = [SceneJsonType, FilePickerFileTypes.All],
        }).ConfigureAwait(true);

        if (files.Count == 0)
            return;

        var path = files[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            notice?.Invoke("Could not resolve file path.");
            return;
        }

        var result = session.Execute(new AgentCommandDto
        {
            ActionId = SceneSessionActionIds.Open,
            Path = path,
        });
        notice?.Invoke(result.Ok ? result.Message : $"Open failed: {result.Message}");
    }

    public static async Task SaveAsync(Control host, SceneSessionService session, Action<string>? notice = null)
    {
        if (string.IsNullOrWhiteSpace(session.DocumentPath))
        {
            await SaveAsAsync(host, session, notice).ConfigureAwait(true);
            return;
        }

        var result = session.Execute(new AgentCommandDto
        {
            ActionId = SceneSessionActionIds.Save,
            Path = session.DocumentPath,
        });
        notice?.Invoke(result.Ok ? result.Message : $"Save failed: {result.Message}");
    }

    public static async Task SaveAsAsync(Control host, SceneSessionService session, Action<string>? notice = null)
    {
        var sp = Storage(host);
        if (sp is null)
        {
            notice?.Invoke("Save As unavailable (no window).");
            return;
        }

        var suggested = string.IsNullOrWhiteSpace(session.DocumentPath)
            ? $"{SanitizeFileName(session.Document.Name)}.nov3djson"
            : Path.GetFileName(session.DocumentPath);

        var file = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Scene As",
            SuggestedFileName = suggested,
            DefaultExtension = "nov3djson",
            FileTypeChoices = [SceneJsonType],
        }).ConfigureAwait(true);

        var path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!path.EndsWith(".nov3djson", StringComparison.OrdinalIgnoreCase))
            path += ".nov3djson";

        var result = session.Execute(new AgentCommandDto
        {
            ActionId = SceneSessionActionIds.Save,
            Path = path,
        });
        notice?.Invoke(result.Ok ? result.Message : $"Save As failed: {result.Message}");
    }

    public static async Task ImportMeshAsync(
        Control host,
        SceneSessionService session,
        Action<string>? notice = null,
        float? targetLengthMeters = null)
    {
        var sp = Storage(host);
        if (sp is null)
        {
            notice?.Invoke("Import unavailable (no window).");
            return;
        }

        var files = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import Mesh (FBX / OBJ / glTF …)",
            AllowMultiple = false,
            FileTypeFilter = [MeshImportType, FilePickerFileTypes.All],
        }).ConfigureAwait(true);

        if (files.Count == 0)
            return;

        var path = files[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            notice?.Invoke("Could not resolve mesh path.");
            return;
        }

        if (!AssimpMeshImporter.IsSupportedExtension(path))
            notice?.Invoke($"Extension may be unsupported by Assimp: {Path.GetExtension(path)}");

        var result = session.Execute(new AgentCommandDto
        {
            ActionId = SceneSessionActionIds.ImportMesh,
            Path = path,
            Name = Path.GetFileNameWithoutExtension(path),
            Distance = targetLengthMeters,
        });
        notice?.Invoke(result.Ok ? result.Message : $"Import failed: {result.Message}");
    }

    private static IStorageProvider? Storage(Control host) =>
        TopLevel.GetTopLevel(host)?.StorageProvider;

    private static async Task RunSafe(Func<Task> work, Action<string>? notice)
    {
        try
        {
            await work().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            notice?.Invoke(ex.Message);
        }
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "untitled";
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim();
    }
}
