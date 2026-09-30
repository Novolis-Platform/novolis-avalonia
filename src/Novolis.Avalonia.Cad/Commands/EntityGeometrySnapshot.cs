using Novolis.Avalonia.Cad.Core;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Cad.Commands;

public sealed class EntityGeometrySnapshot
{
    public float[]? A { get; init; }
    public float[]? B { get; init; }
    public float[]? Center { get; init; }
    public float[]? HalfExtents { get; init; }
    public float Radius { get; init; }
    public float Height { get; init; }
    public List<float[]>? ControlPoints { get; init; }
    public float[]? Knots { get; init; }
    public float[]? Weights { get; init; }
    public List<float[]>? FitPoints { get; init; }
    public bool Closed { get; init; }

    public static EntityGeometrySnapshot Capture(CadEntity e) => new()
    {
        A = Clone(e.A),
        B = Clone(e.B),
        Center = Clone(e.Center),
        HalfExtents = Clone(e.HalfExtents),
        Radius = e.Radius,
        Height = e.Height,
        ControlPoints = e.ControlPoints?.Select(Clone!).Where(p => p is not null).Cast<float[]>().ToList(),
        Knots = Clone(e.Knots),
        Weights = Clone(e.Weights),
        FitPoints = e.FitPoints?.Select(Clone!).Where(p => p is not null).Cast<float[]>().ToList(),
        Closed = e.Closed,
    };

    public void ApplyTo(CadEntity e)
    {
        e.A = Clone(A);
        e.B = Clone(B);
        e.Center = Clone(Center);
        e.HalfExtents = Clone(HalfExtents);
        e.Radius = Radius;
        e.Height = Height;
        e.ControlPoints = ControlPoints?.Select(Clone!).Where(p => p is not null).Cast<float[]>().ToList();
        e.Knots = Clone(Knots);
        e.Weights = Clone(Weights);
        e.FitPoints = FitPoints?.Select(Clone!).Where(p => p is not null).Cast<float[]>().ToList();
        e.Closed = Closed;
    }

    private static float[]? Clone(float[]? v) => v is null ? null : (float[])v.Clone();
}
