using Novolis.Cad.Primitives;
using Novolis.Ship.Design;

namespace Novolis.Avalonia.Ship.Design.Grips;

/// <summary>Baseline §20 grip descriptors for selected semantic objects (PLAN direct manipulation).</summary>
public sealed record ShipGrip(
    ShipObjectId ObjectId,
    ShipGripKind Kind,
    float X,
    float Y,
    float Z,
    string Label);
