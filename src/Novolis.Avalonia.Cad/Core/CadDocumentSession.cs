using System.Text.Json;
using System.Text.Json.Serialization;
using Novolis.Cad.Hierarchy;
using Novolis.Cad.Primitives;
using Novolis.Cad.Visibility;

namespace Novolis.Avalonia.Cad.Core;

public sealed class CadDocumentSession
{
    private readonly CadEditorSettings _settings;
    private readonly CadHierarchyRegistry _plainHierarchy = new();
    private readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    public CadDocument Document { get; private set; } = new();

    public bool IsDirty { get; private set; }

    public Guid? SelectedId { get; set; }

    /// <summary>Ordered multi-selection (Connect, Bridge). First entry mirrors <see cref="SelectedId"/> when set.</summary>
    public List<Guid> SelectedIds { get; } = [];

    /// <summary>Optional semantic assembly/part hierarchy for the active document.</summary>
    public CadHierarchyRegistry? Hierarchy { get; private set; }

    /// <summary>Visibility state for the active semantic hierarchy.</summary>
    public HierarchyVisibilityService Visibility { get; } = new();

    public string WorkspacePath => _settings.WorkspacePath;

    /// <summary>Active document path (may differ from the default workspace file).</summary>
    public string DocumentPath { get; private set; }

    public event Action? Changed;

    public CadDocumentSession(CadEditorSettings settings)
    {
        _settings = settings;
        DocumentPath = settings.DocumentPath;
        Visibility.Changed += Notify;
    }

    public void OpenOrCreateDefault()
    {
        _settings.Load();
        Directory.CreateDirectory(_settings.WorkspacePath);
        var preferred = _settings.Settings.LastDocumentPath;
        var path = !string.IsNullOrWhiteSpace(preferred) && File.Exists(preferred)
            ? preferred!
            : _settings.DocumentPath;
        var legacy = Path.Combine(_settings.WorkspacePath, "draft.json");
        if (File.Exists(path))
        {
            OpenFromPath(path);
            return;
        }

        if (File.Exists(legacy))
        {
            // Prefer new path; leave legacy file in place.
            Document = CreateStarter();
            ClearHierarchyBinding();
            DocumentPath = _settings.DocumentPath;
            Save();
            return;
        }

        Document = CreateStarter();
        ClearHierarchyBinding();
        DocumentPath = _settings.DocumentPath;
        Save();
    }

    public void NewDocument()
    {
        Document = CreateStarter();
        ClearHierarchyBinding();
        DocumentPath = _settings.DocumentPath;
        SelectedId = null;
        SelectedIds.Clear();
        IsDirty = true;
        Notify();
    }

    public void OpenFromPath(string path)
    {
        var text = File.ReadAllText(path);
        Document = JsonSerializer.Deserialize<CadDocument>(text, _json) ?? CreateStarter();
        CadVec.EnsureDefaultLayer(Document);
        ClearHierarchyBinding();
        DocumentPath = path;
        _settings.Settings.LastDocumentPath = path;
        SelectedId = null;
        SelectedIds.Clear();
        IsDirty = false;
        Notify();
    }

    /// <summary>Binds semantic hierarchy and visibility to the current document.</summary>
    public void BindHierarchy(CadHierarchyRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        Hierarchy = registry;
        Visibility.BindHierarchy(registry, Document);
    }

    /// <summary>Whole-document replace (generator policy). Clears selection; optional dirty flag.</summary>
    public void ReplaceDocument(CadDocument document, bool markDirty = true)
    {
        ArgumentNullException.ThrowIfNull(document);
        Document = document;
        CadVec.EnsureDefaultLayer(Document);
        SelectedId = null;
        SelectedIds.Clear();
        ClearHierarchyBinding();
        IsDirty = markDirty;
        Notify();
    }

    public void Save() => SaveTo(DocumentPath);

    public void SaveTo(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        Document.Format = "novolis.cad";
        Document.SchemaVersion = 1;
        Document.LinearUnit = "meter";
        if (Document.UnitScaleMeters <= 0)
            Document.UnitScaleMeters = 1f;
        Document.ModifiedAt = DateTime.UtcNow.ToString("O");
        Document.CreatedAt ??= Document.ModifiedAt;
        Document.Generator = new CadGenerator { Name = "Novolis.Avalonia.Cad", Version = "2026.1.0" };
        CadVec.EnsureDefaultLayer(Document);
        File.WriteAllText(path, JsonSerializer.Serialize(Document, _json));
        DocumentPath = path;
        _settings.Settings.LastDocumentPath = path;
        IsDirty = false;
        Notify();
    }

    public void MarkDirty()
    {
        IsDirty = true;
        Notify();
    }

    public void Notify() => Changed?.Invoke();

    public void SetSelection(Guid? id, bool additive = false)
    {
        if (!additive)
            SelectedIds.Clear();
        SelectedId = id;
        if (id is { } g)
        {
            if (!SelectedIds.Contains(g))
                SelectedIds.Add(g);
        }

        Notify();
    }

    public CadEntity? SelectedEntity =>
        SelectedId is { } id ? Document.Entities.FirstOrDefault(e => e.Id == id) : null;

    private void ClearHierarchyBinding()
    {
        while (Visibility.IsolationActive)
            Visibility.EndIsolation();

        Visibility.ClearAllOverrides(recordUndo: false);
        Hierarchy = null;
        Visibility.BindHierarchy(_plainHierarchy, Document);
    }

    public static CadDocument CreateStarter()
    {
        var layerId = Guid.Parse("a0000000-0000-4000-8000-000000000001");
        var now = DateTime.UtcNow.ToString("O");
        var doc = new CadDocument
        {
            Name = "Starter sketch",
            CreatedAt = now,
            ModifiedAt = now,
            Layers =
            [
                new CadLayer
                {
                    Id = layerId,
                    Name = "0",
                    Visible = true,
                    Color = [0.8f, 0.8f, 0.8f],
                },
            ],
        };

        doc.Entities.Add(new CadEntity
        {
            Name = "Baseline",
            Kind = "line",
            LayerId = layerId,
            A = CadVec.Xz(0, -2),
            B = CadVec.Xz(0, 2),
            Color = [0.7f, 0.75f, 0.9f],
            Style = new CadStyle { Linetype = "Continuous", LineWeightMm = 0.25f },
        });
        doc.Entities.Add(new CadEntity
        {
            Name = "Origin circle",
            Kind = "circle",
            LayerId = layerId,
            Center = CadVec.Xz(0, 0),
            Radius = 1f,
            Normal = [0f, 1f, 0f],
            Color = [0.55f, 0.8f, 0.7f],
        });
        return doc;
    }
}
