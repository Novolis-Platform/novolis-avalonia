using Novolis.Cad.Primitives;
using Novolis.Ship.Analysis;
using Novolis.Ship.Design;
using Novolis.Ship.Validation;

namespace Novolis.Avalonia.Ship.Design.Session;

/// <summary>Mutable session over a <see cref="ShipDesign"/> (parallel to CadDocumentSession).</summary>
public sealed class ShipDesignSession
{
    private ShipDesign _design;
    private string? _path;
    private ShipValidationResult _validation = new() { Issues = [] };
    private ShipAnalysisReport _analysis = EmptyAnalysis();

    public ShipDesignSession(string dataRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataRoot);
        DataRoot = dataRoot;
        Directory.CreateDirectory(dataRoot);
        _design = ShipFactory.Create(DefaultDefinition("New Ship"));
        Recompute();
    }

    public string DataRoot { get; }

    public ShipDesign Design => _design;

    public string? Path => _path;

    public ShipObjectId? SelectedObjectId { get; private set; }

    public ShipWorkspaceKind Workspace { get; private set; } = ShipWorkspaceKind.Plan;

    public int ActiveDeckIndex { get; private set; }

    public AnalysisCategory? ActiveAnalysisCategory { get; private set; }

    public string? ActiveLoadCaseId { get; private set; }

    public Guid? BreachCompartmentId { get; private set; }

    public ShipValidationResult Validation => _validation;

    public ShipAnalysisReport Analysis => _analysis;

    public bool SnapEnabled { get; set; } = true;

    public float SnapGridMeters { get; set; } = 0.25f;

    public bool ShowDimensions { get; set; } = true;

    public bool ShowStructuralOverlays { get; set; } = true;

    public event Action? Changed;

    public void NewShip(ShipDefinition definition)
    {
        _design = ShipFactory.Create(definition);
        _path = null;
        SelectedObjectId = _design.Hull.Id.AsObject();
        ActiveDeckIndex = 0;
        ActiveLoadCaseId = _design.LoadCases.FirstOrDefault()?.Id;
        Notify();
    }

    public void Replace(ShipDesign design, string? path = null)
    {
        ArgumentNullException.ThrowIfNull(design);
        _design = design;
        _path = path;
        ActiveLoadCaseId ??= _design.LoadCases.FirstOrDefault()?.Id;
        Notify();
    }

    public void Mutate(Func<ShipDesign, ShipDesign> mutator)
    {
        ArgumentNullException.ThrowIfNull(mutator);
        _design = mutator(_design);
        Notify();
    }

    public void Select(ShipObjectId? id)
    {
        SelectedObjectId = id;
        Notify();
    }

    public void SetWorkspace(ShipWorkspaceKind workspace)
    {
        Workspace = workspace;
        Notify();
    }

    public void SetActiveDeck(int index)
    {
        if (_design.Decks.Count == 0)
            return;
        ActiveDeckIndex = System.Math.Clamp(index, 0, _design.Decks.Count - 1);
        Notify();
    }

    public void SetAnalysisCategory(AnalysisCategory? category)
    {
        ActiveAnalysisCategory = category;
        Notify();
    }

    public void SetActiveLoadCase(string? loadCaseId)
    {
        if (string.Equals(ActiveLoadCaseId, loadCaseId, StringComparison.Ordinal))
            return;
        ActiveLoadCaseId = loadCaseId;
        Notify();
    }

    public void SetBreachCompartment(Guid? compartmentId)
    {
        if (BreachCompartmentId == compartmentId)
            return;
        BreachCompartmentId = compartmentId;
        Notify();
    }

    public void Save()
    {
        var path = _path ?? System.IO.Path.Combine(DataRoot, SanitizeFileName(_design.Ship.Name) + ".shipjson");
        SaveTo(path);
    }

    public void SaveTo(string path)
    {
        ShipDesignStore.Save(_design, path);
        _path = path;
        Notify();
    }

    public void OpenFromPath(string path)
    {
        _design = ShipDesignStore.Load(path);
        _path = path;
        SelectedObjectId = _design.Hull.Id.AsObject();
        ActiveDeckIndex = 0;
        ActiveLoadCaseId = _design.LoadCases.FirstOrDefault()?.Id;
        Notify();
    }

    public void ImportCadDocument(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _design = ShipCadProjector.FromCadDocument(document);
        _path = null;
        SelectedObjectId = _design.Hull.Id.AsObject();
        ActiveLoadCaseId = _design.LoadCases.FirstOrDefault()?.Id;
        Notify();
    }

    public CadDocument? SelectedObjectGeometry()
    {
        if (SelectedObjectId is null)
            return null;
        var id = SelectedObjectId.Value.Value;
        foreach (var (oid, geom, _) in _design.GeometricObjects())
        {
            if (oid.Value == id)
                return geom;
        }

        return null;
    }

    public void Notify()
    {
        Recompute();
        Changed?.Invoke();
    }

    private void Recompute()
    {
        _validation = ShipDesignValidator.Validate(_design);
        _analysis = ShipAnalyzer.Analyze(_design, new ShipAnalysisContext
        {
            ActiveLoadCaseId = ActiveLoadCaseId,
            BreachCompartmentId = BreachCompartmentId,
        });
    }

    public static ShipDefinition DefaultDefinition(string name) => new()
    {
        Name = name,
        Length = ShipLengths.FromMeters(90f),
        Beam = ShipLengths.FromMeters(20f),
        Height = ShipLengths.FromMeters(12f),
        DeckCount = 3,
        DeckSpacing = ShipLengths.FromMeters(4f),
        HullMaterial = MaterialId.Steel,
        HullThickness = ShipLengths.FromMeters(0.024f),
        FrameSpacing = ShipLengths.FromMeters(1.5f),
        PrimaryStructuralMaterial = MaterialId.Steel,
        HullGenerator = HullGeneratorKind.Faceted,
        GravitySystem = GravitySystemKind.Plating,
        NominalGravityG = 1f,
        NominalInternalPressureAtm = 1f,
        ExternalEnvironment = ExternalEnvironmentKind.Vacuum,
    };

    private static ShipAnalysisReport EmptyAnalysis() => new()
    {
        Categories = [],
        Findings = [],
        TotalMassKg = 0,
        CenterOfMassX = 0,
        CenterOfMassY = 0,
        CenterOfMassZ = 0,
    };

    private static string SanitizeFileName(string name)
    {
        foreach (var c in System.IO.Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "ship" : name.Trim();
    }
}
