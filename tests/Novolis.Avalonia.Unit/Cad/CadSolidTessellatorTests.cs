using System.Numerics;
using Novolis.Avalonia.Cad.Evaluation;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Unit.Cad;

public sealed class CadSolidTessellatorTests
{
    [Test]
    public async Task Box_Sphere_Cylinder_ProduceTriangles()
    {
        var box = CadSolidTessellator.Box(new Vector3(1, 1, 1));
        await Assert.That(box.TriangleCount).IsGreaterThan(0);

        var sphere = CadSolidTessellator.Sphere(1f, 8, 12);
        await Assert.That(sphere.TriangleCount).IsGreaterThan(0);

        var cylinder = CadSolidTessellator.Cylinder(0.5f, 2f, 12);
        await Assert.That(cylinder.TriangleCount).IsGreaterThan(0);
    }

    [Test]
    public async Task TryTessellate_And_StoreRoundTrip()
    {
        var entity = new CadEntity
        {
            Kind = "box",
            Center = [0f, 0f, 0f],
            HalfExtents = [0.5f, 0.5f, 0.5f],
        };
        var mesh = CadSolidTessellator.TryTessellate(entity);
        await Assert.That(mesh).IsNotNull();
        CadSolidTessellator.StoreOnEntity(entity, mesh!);
        var restored = CadSolidTessellator.FromStored(entity);
        await Assert.That(restored.TriangleCount).IsEqualTo(mesh!.TriangleCount);
    }
}
