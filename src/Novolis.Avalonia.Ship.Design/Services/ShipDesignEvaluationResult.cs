using Novolis.Cad.Evaluation;
using Novolis.Cad.Primitives;
using Novolis.Math.Geometry;
using Novolis.Ship.Design;
using Novolis.Modeling;

namespace Novolis.Avalonia.Ship.Design.Services;

public sealed class ShipDesignEvaluationResult
{
    public required int ObjectCount { get; init; }
    public required int CutoutCount { get; init; }
    public required int MeshNodeCount { get; init; }
    public required CadDocument FlatCad { get; init; }
    public required SceneDocument Scene { get; init; }
    public required string? ScenePath { get; init; }
}
