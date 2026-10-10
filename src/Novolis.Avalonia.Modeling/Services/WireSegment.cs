using System.Numerics;
using Novolis.Avalonia.Modeling.Session;
using Novolis.Modeling;

namespace Novolis.Avalonia.Modeling.Services;

/// <summary>World-space line for CAD wire presenters (mesh edges, grid, light/camera gizmos).</summary>
public readonly record struct WireSegment(Vector3 A, Vector3 B, byte R, byte G, byte Blue);
