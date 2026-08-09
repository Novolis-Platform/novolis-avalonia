using Novolis.Cad.Primitives;
using Novolis.Cad.SceneBridge;
using Novolis.Ship.Design;

namespace Novolis.Avalonia.Ship.Design.Services;

public sealed class ShipDesignEvaluationResult
{
    public required int ObjectCount { get; init; }
    public required int CutoutCount { get; init; }
    public required CadDocument FlatCad { get; init; }
    public required string? ScenePath { get; init; }
}

/// <summary>
/// Projects <see cref="ShipDesign"/> to flat CAD and bridges to a scene file via Cad.SceneBridge.
/// </summary>
public static class ShipDesignEvaluator
{
    public static ShipDesignEvaluationResult Evaluate(ShipDesign design, string? scenePath = null)
    {
        ArgumentNullException.ThrowIfNull(design);
        var flat = ShipCadProjector.ToCadDocument(design);
        if (!string.IsNullOrWhiteSpace(scenePath))
            CadSceneBridge.SaveNov3dJson(flat, scenePath);
        return new ShipDesignEvaluationResult
        {
            ObjectCount = design.GeometricObjects().Count(),
            CutoutCount = design.Cutouts.Count,
            FlatCad = flat,
            ScenePath = scenePath,
        };
    }
}
