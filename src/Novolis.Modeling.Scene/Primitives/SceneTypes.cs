using System.Numerics;
using System.Text.Json.Serialization;

namespace Novolis.Modeling.Scene;

/// <summary>Light classification for Look-stage nodes.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LightKind
{
    Omni,
    Spot,
    Infinite,
    Area,
}

/// <summary>Mesh primitive kinds for v1 scaffolding.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MeshPrimitiveKind
{
    Box,
    Sphere,
    Plane,
    Cylinder,
}

/// <summary>Generator kinds (phase 2).</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GeneratorKind
{
    Cloner,
    Symmetry,
    Extrude,
}

/// <summary>Modifier kinds (phase 2).</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ModifierKind
{
    Weld,
    Subdivision,
    Optimize,
}

/// <summary>Local transform (translation, euler degrees, scale).</summary>
public sealed class SceneTransform
{
    public float[] Position { get; set; } = [0, 0, 0];
    public float[] RotationDeg { get; set; } = [0, 0, 0];
    public float[] Scale { get; set; } = [1, 1, 1];

    public Vector3 PositionV => new(Position[0], Position[1], Position[2]);
    public Vector3 RotationDegV => new(RotationDeg[0], RotationDeg[1], RotationDeg[2]);
    public Vector3 ScaleV => new(Scale[0], Scale[1], Scale[2]);

    public SceneTransform Clone() => new()
    {
        Position = [Position[0], Position[1], Position[2]],
        RotationDeg = [RotationDeg[0], RotationDeg[1], RotationDeg[2]],
        Scale = [Scale[0], Scale[1], Scale[2]],
    };

    public Matrix4x4 ToMatrix()
    {
        var t = Matrix4x4.CreateTranslation(PositionV);
        var rx = Matrix4x4.CreateRotationX(RotationDeg[0] * MathF.PI / 180f);
        var ry = Matrix4x4.CreateRotationY(RotationDeg[1] * MathF.PI / 180f);
        var rz = Matrix4x4.CreateRotationZ(RotationDeg[2] * MathF.PI / 180f);
        var s = Matrix4x4.CreateScale(ScaleV);
        return s * rx * ry * rz * t;
    }
}
