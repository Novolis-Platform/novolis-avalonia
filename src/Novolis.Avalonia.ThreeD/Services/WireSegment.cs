using System.Numerics;
using Novolis.Avalonia.ThreeD.Session;
using Novolis.ThreeD;

namespace Novolis.Avalonia.ThreeD.Services;

/// <summary>World-space line for CAD wire presenters (mesh edges, grid, light/camera gizmos).</summary>
public readonly record struct WireSegment(Vector3 A, Vector3 B, byte R, byte G, byte Blue);
