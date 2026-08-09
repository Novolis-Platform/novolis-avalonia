using System.Numerics;
using Novolis.Avalonia.Cad.Commands;
using Novolis.Avalonia.Cad.Core;
using Novolis.Cad.Evaluation;
using Novolis.Avalonia.Cad.Services;
using Novolis.Avalonia.Cad.Session;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Unit.Cad;

public sealed class CadBooleanArrayInstanceTests
{
    private static CadEntity Box(Guid id, float[] center, float[] half) => new()
    {
        Id = id,
        Kind = "box",
        Center = center,
        HalfExtents = half,
    };

    [Test]
    public async Task Boolean_Union_Intersect_Difference_Semantics()
    {
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var cId = Guid.NewGuid();
        // Large target; small cutter centered on +X face so difference removes that face's tris.
        var a = Box(aId, [0f, 0f, 0f], [1f, 1f, 1f]);
        var faceCutter = Box(bId, [1f, 0f, 0f], [0.25f, 1.1f, 1.1f]);
        // Region covering +X face centroid for intersection keep.
        var region = Box(cId, [1f, 0f, 0f], [0.3f, 1.2f, 1.2f]);
        var unionId = Guid.NewGuid();
        var intersectId = Guid.NewGuid();
        var diffId = Guid.NewGuid();
        var doc = new CadDocument();
        doc.Entities.AddRange(
        [
            a, faceCutter, region,
            new CadEntity
            {
                Id = unionId, Kind = "boolean", Operation = "union", Mode = "solid",
                TargetId = aId, CutterId = bId, LeftId = aId, RightId = bId,
            },
            new CadEntity
            {
                Id = intersectId, Kind = "boolean", Operation = "intersect", Mode = "solid",
                TargetId = aId, CutterId = cId, LeftId = aId, RightId = cId,
            },
            new CadEntity
            {
                Id = diffId, Kind = "boolean", Operation = "subtract", Mode = "solid",
                TargetId = aId, CutterId = bId, LeftId = aId, RightId = bId,
            },
        ]);

        var cache = new CadModelEvaluator().Evaluate(doc);
        var leftTris = cache.CadMeshes[aId].TriangleCount;
        await Assert.That(cache.CadMeshes[unionId].TriangleCount).IsEqualTo(leftTris + cache.CadMeshes[bId].TriangleCount);
        await Assert.That(cache.CadMeshes[intersectId].TriangleCount).IsGreaterThan(0);
        await Assert.That(cache.CadMeshes[intersectId].TriangleCount).IsLessThan(leftTris);
        await Assert.That(cache.CadMeshes[diffId].TriangleCount).IsLessThan(leftTris);
    }

    [Test]
    public async Task NestedBoolean_EvaluatesWhenConsumerListedFirst()
    {
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var cId = Guid.NewGuid();
        var innerId = Guid.NewGuid();
        var outerId = Guid.NewGuid();
        var a = Box(aId, [0f, 0f, 0f], [2f, 1f, 1f]);
        var b = Box(bId, [0.5f, 0f, 0f], [0.5f, 0.5f, 0.5f]);
        var c = Box(cId, [-0.5f, 0f, 0f], [0.4f, 0.4f, 0.4f]);
        var inner = new CadEntity
        {
            Id = innerId, Kind = "boolean", Operation = "subtract", Mode = "solid",
            TargetId = aId, CutterId = bId, LeftId = aId, RightId = bId,
        };
        var outer = new CadEntity
        {
            Id = outerId, Kind = "boolean", Operation = "subtract", Mode = "solid",
            TargetId = innerId, CutterId = cId, LeftId = innerId, RightId = cId,
        };
        // Consumer listed before producer — topo-sort must still succeed.
        var doc = new CadDocument();
        doc.Entities.AddRange([outer, a, b, c, inner]);

        var ordered = CadModelEvaluator.OrderForCadEvaluation(doc.Entities);
        var outerIdx = ordered.FindIndex(e => e.Id == outerId);
        var innerIdx = ordered.FindIndex(e => e.Id == innerId);
        await Assert.That(innerIdx).IsLessThan(outerIdx);

        var cache = new CadModelEvaluator().Evaluate(doc);
        await Assert.That(cache.CadMeshes.ContainsKey(innerId)).IsTrue();
        await Assert.That(cache.CadMeshes.ContainsKey(outerId)).IsTrue();
        await Assert.That(cache.CadMeshes[outerId].TriangleCount).IsGreaterThan(0);
    }

    [Test]
    public async Task Connect_FuseSolid_UsesEvaluatedBooleanMeshes()
    {
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var boolId = Guid.NewGuid();
        var connectId = Guid.NewGuid();
        var a = Box(aId, [0f, 0f, 0f], [1f, 1f, 1f]);
        var b = Box(bId, [3f, 0f, 0f], [0.5f, 0.5f, 0.5f]);
        var boolean = new CadEntity
        {
            Id = boolId, Kind = "boolean", Operation = "union", Mode = "solid",
            TargetId = aId, CutterId = bId, LeftId = aId, RightId = bId,
        };
        var connect = new CadEntity
        {
            Id = connectId, Kind = "connect", Mode = "fuseSolid",
            MemberIds = [boolId, bId],
        };
        var doc = new CadDocument();
        doc.Entities.AddRange([a, b, boolean, connect]);

        var cache = new CadModelEvaluator().Evaluate(doc);
        await Assert.That(cache.CadMeshes.ContainsKey(connectId)).IsTrue();
        await Assert.That(cache.CadMeshes[connectId].TriangleCount)
            .IsGreaterThanOrEqualTo(cache.CadMeshes[boolId].TriangleCount);
    }

    [Test]
    public async Task Array_LinearInstances_FusedSolid_SeparateCopies()
    {
        var srcId = Guid.NewGuid();

        var instanceOnly = new CadDocument();
        instanceOnly.Entities.AddRange(
        [
            Box(srcId, [0f, 0f, 0f], [0.2f, 0.2f, 0.2f]),
            new CadEntity
            {
                Id = Guid.NewGuid(), Kind = "arrayInstance", PrototypeId = srcId, SourceId = srcId,
                Counts = [3, 1, 1], Spacing = [1f, 0f, 0f], Realization = "instances",
            },
        ]);
        var instCache = new CadModelEvaluator().Evaluate(instanceOnly);
        await Assert.That(instCache.Instances.Count).IsEqualTo(3);
        await Assert.That(instCache.Instances.All(i => i.Mesh is not null)).IsTrue();
        await Assert.That(ReferenceEquals(instCache.Instances[0].Mesh, instCache.Instances[1].Mesh)).IsTrue();

        var fusedId = Guid.NewGuid();
        var fusedOnly = new CadDocument();
        fusedOnly.Entities.AddRange(
        [
            Box(srcId, [0f, 0f, 0f], [0.2f, 0.2f, 0.2f]),
            new CadEntity
            {
                Id = fusedId, Kind = "arrayInstance", PrototypeId = srcId, SourceId = srcId,
                Counts = [2, 2, 1], Spacing = [1f, 1f, 0f], Realization = "fusedSolid",
            },
        ]);
        var fusedCache = new CadModelEvaluator().Evaluate(fusedOnly);
        await Assert.That(fusedCache.ModeledMeshes.ContainsKey(fusedId)).IsTrue();
        await Assert.That(fusedCache.ModeledMeshes[fusedId].TriangleCount)
            .IsEqualTo(fusedCache.CadMeshes[srcId].TriangleCount * 4);
        await Assert.That(fusedCache.Instances.Count).IsEqualTo(0);

        var sepId = Guid.NewGuid();
        var sepOnly = new CadDocument();
        sepOnly.Entities.AddRange(
        [
            Box(srcId, [0f, 0f, 0f], [0.2f, 0.2f, 0.2f]),
            new CadEntity
            {
                Id = sepId, Kind = "arrayInstance", PrototypeId = srcId, SourceId = srcId,
                Counts = [2, 1, 1], Spacing = [2f, 0f, 0f], Realization = "separateCopies",
            },
        ]);
        var sepCache = new CadModelEvaluator().Evaluate(sepOnly);
        await Assert.That(sepCache.Instances.Count).IsEqualTo(2);
        await Assert.That(sepCache.Instances[0].Mesh).IsNotNull();
        await Assert.That(sepCache.Instances[1].Mesh).IsNotNull();
        await Assert.That(ReferenceEquals(sepCache.Instances[0].Mesh, sepCache.Instances[1].Mesh)).IsFalse();
        await Assert.That(sepCache.ModeledMeshes.ContainsKey(sepId)).IsTrue();
    }

    [Test]
    public async Task Array_Radial_ExpandPattern()
    {
        var srcId = Guid.NewGuid();
        var arrId = Guid.NewGuid();
        var doc = new CadDocument();
        doc.Entities.AddRange(
        [
            Box(srcId, [1f, 0f, 0f], [0.1f, 0.1f, 0.1f]),
            new CadEntity
            {
                Id = arrId, Kind = "arrayInstance", PrototypeId = srcId, SourceId = srcId,
                Counts = [4], Axis = [0f, 1f, 0f], StepRadians = MathF.PI / 2f, Realization = "instances",
            },
        ]);
        var cache = new CadModelEvaluator().Evaluate(doc);
        await Assert.That(cache.Instances.Count).IsEqualTo(4);
    }

    [Test]
    public async Task SingleInstance_PlacesPrototype()
    {
        var srcId = Guid.NewGuid();
        var instId = Guid.NewGuid();
        var doc = new CadDocument();
        doc.Entities.AddRange(
        [
            Box(srcId, [0f, 0f, 0f], [0.5f, 0.5f, 0.5f]),
            new CadEntity
            {
                Id = instId, Kind = "instance", PrototypeId = srcId,
                Transform = new CadTransform { Center = [2f, 0f, 0f] },
            },
        ]);
        var cache = new CadModelEvaluator().Evaluate(doc);
        await Assert.That(cache.Instances.Count).IsEqualTo(1);
        await Assert.That(cache.Instances[0].SourceId).IsEqualTo(srcId);
        await Assert.That(cache.Instances[0].Transform.M41).IsEqualTo(2f);
    }

    [Test]
    public async Task Session_Boolean_Instance_RadialClone()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-cad-bool-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var settings = new CadEditorSettings(root);
            var session = new CadDocumentSession(settings);
            session.NewDocument();
            var target = new CadEntity
            {
                Kind = "box", Center = [0f, 0.5f, 0f], HalfExtents = [1f, 0.5f, 1f],
            };
            var cutter = new CadEntity
            {
                Kind = "box", Center = [0.4f, 0.5f, 0f], HalfExtents = [0.3f, 0.3f, 0.3f],
            };
            var bus = new CadCommandBus(session);
            bus.Execute(new AddEntityCommand(target));
            bus.Execute(new AddEntityCommand(cutter));
            var cad = new CadSessionService(session, settings, bus, new CadCommandDispatcher(session, bus, settings));

            var boolean = cad.Execute(new CadCommandDto
            {
                ActionId = CadSessionActionIds.Boolean,
                TargetId = target.Id,
                CutterId = cutter.Id,
                Operation = "subtract",
            });
            await Assert.That(boolean.Ok).IsTrue();
            var boolEntity = session.Document.Entities.Single(e => e.Kind == "boolean");

            var inst = cad.Execute(new CadCommandDto
            {
                ActionId = CadSessionActionIds.Instance,
                PrototypeId = target.Id,
                Center = [3f, 0f, 0f],
            });
            await Assert.That(inst.Ok).IsTrue();
            await Assert.That(session.Document.Entities.Any(e => e.Kind == "instance")).IsTrue();

            var radial = cad.Execute(new CadCommandDto
            {
                ActionId = CadSessionActionIds.Clone,
                SourceId = target.Id,
                Counts = [6],
                Axis = [0f, 1f, 0f],
                StepRadians = MathF.PI / 3f,
                Realization = "instances",
            });
            await Assert.That(radial.Ok).IsTrue();
            var cloner = session.Document.Entities.Single(e => e.Kind == "arrayInstance");
            await Assert.That(cloner.Axis).IsNotNull();
            await Assert.That(cloner.StepRadians).IsEqualTo(MathF.PI / 3f);

            var cache = new CadModelEvaluator().Evaluate(session.Document);
            await Assert.That(cache.CadMeshes.ContainsKey(boolEntity.Id)).IsTrue();
            await Assert.That(cache.Instances.Count).IsGreaterThanOrEqualTo(7); // 1 instance + 6 radial
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }
}
