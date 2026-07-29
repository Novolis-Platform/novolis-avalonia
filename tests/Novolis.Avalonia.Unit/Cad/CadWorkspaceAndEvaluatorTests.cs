using Novolis.Avalonia.Cad.Commands;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Evaluation;
using Novolis.Avalonia.Cad.Scene;
using Novolis.Avalonia.Cad.Services;
using Novolis.Avalonia.Cad.Session;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Unit.Cad;

public sealed class CadWorkspaceAndEvaluatorTests
{
    [Test]
    public async Task WorkspaceMapping_ParsesAliases()
    {
        await Assert.That(CadWorkspaceMapping.Parse("draft")).IsEqualTo(CadWorkspace.Cad);
        await Assert.That(CadWorkspaceMapping.Parse("model")).IsEqualTo(CadWorkspace.Preview);
        await Assert.That(CadWorkspaceMapping.Parse("modeling")).IsEqualTo(CadWorkspace.Modeling);
        await Assert.That(CadWorkspaceMapping.Parse("preview")).IsEqualTo(CadWorkspace.Preview);
    }

    [Test]
    public async Task SceneGraph_ClassifiesNodes()
    {
        var box = new CadEntity { Kind = "box", Name = "Body" };
        var boolOp = new CadEntity { Kind = "boolean", Operation = "subtract" };
        var mesh = new CadEntity { Kind = "meshFromSolid", LinkMode = "linked" };
        await Assert.That(CadSceneGraph.Classify(box)).IsEqualTo(CadSceneNodeCategory.Geometry);
        await Assert.That(CadSceneGraph.Classify(boolOp)).IsEqualTo(CadSceneNodeCategory.Generator);
        await Assert.That(CadSceneGraph.Classify(mesh)).IsEqualTo(CadSceneNodeCategory.MeshFromSolid);
    }

    [Test]
    public async Task Evaluator_BooleanAndSymmetry_ProduceMeshes()
    {
        var doc = new CadDocument();
        var a = new CadEntity
        {
            Id = Guid.NewGuid(),
            Kind = "box",
            Center = [0f, 0.5f, 0f],
            HalfExtents = [1f, 0.5f, 1f],
        };
        var b = new CadEntity
        {
            Id = Guid.NewGuid(),
            Kind = "box",
            Center = [0.5f, 0.5f, 0f],
            HalfExtents = [0.4f, 0.4f, 0.4f],
        };
        var boolean = new CadEntity
        {
            Id = Guid.NewGuid(),
            Kind = "boolean",
            Operation = "subtract",
            Mode = "solid",
            TargetId = a.Id,
            CutterId = b.Id,
            LeftId = a.Id,
            RightId = b.Id,
        };
        var symmetry = new CadEntity
        {
            Id = Guid.NewGuid(),
            Kind = "symmetry",
            SourceId = a.Id,
            Normal = [1f, 0f, 0f],
            PlanePoint = [0f, 0f, 0f],
            MergeAtPlane = true,
        };
        doc.Entities.AddRange([a, b, boolean, symmetry]);

        var eval = new CadModelEvaluator();
        var cache = eval.Evaluate(doc);
        await Assert.That(cache.CadMeshes.ContainsKey(boolean.Id)).IsTrue();
        await Assert.That(cache.CadMeshes.ContainsKey(symmetry.Id)).IsTrue();
        await Assert.That(cache.CadMeshes[boolean.Id].TriangleCount).IsGreaterThan(0);
    }

    [Test]
    public async Task Evaluator_MeshFromSolid_Linked()
    {
        var doc = new CadDocument();
        var box = new CadEntity
        {
            Id = Guid.NewGuid(),
            Kind = "box",
            Center = [0f, 0f, 0f],
            HalfExtents = [0.5f, 0.5f, 0.5f],
        };
        var adapter = new CadEntity
        {
            Id = Guid.NewGuid(),
            Kind = "meshFromSolid",
            SourceId = box.Id,
            LinkMode = "linked",
        };
        doc.Entities.AddRange([box, adapter]);
        var cache = new CadModelEvaluator().Evaluate(doc);
        await Assert.That(cache.ModeledMeshes.ContainsKey(adapter.Id)).IsTrue();
    }

    [Test]
    public async Task Session_SetWorkspace_And_SymmetryAction()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-cad-ws-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var settings = new CadEditorSettings(root);
            var session = new CadDocumentSession(settings);
            session.NewDocument();
            var box = new CadEntity
            {
                Kind = "box",
                Center = [0f, 0.5f, 0f],
                HalfExtents = [1f, 0.5f, 1f],
            };
            var bus = new CadCommandBus(session);
            bus.Execute(new AddEntityCommand(box));
            var dispatcher = new CadCommandDispatcher(session, bus, settings);
            var cad = new CadSessionService(session, settings, bus, dispatcher);

            var ws = cad.Execute(new CadCommandDto
            {
                ActionId = CadSessionActionIds.SetWorkspace,
                Workspace = "modeling",
            });
            await Assert.That(ws.Ok).IsTrue();
            await Assert.That(cad.Snapshot().Workspace).IsEqualTo("modeling");

            var sym = cad.Execute(new CadCommandDto
            {
                ActionId = CadSessionActionIds.Symmetry,
                SourceId = box.Id,
                MergeAtPlane = true,
            });
            await Assert.That(sym.Ok).IsTrue();
            await Assert.That(session.Document.Entities.Any(e => e.Kind == "symmetry")).IsTrue();

            var mesh = cad.Execute(new CadCommandDto
            {
                ActionId = CadSessionActionIds.MeshFromSolid,
                SourceId = box.Id,
                LinkMode = "linked",
            });
            await Assert.That(mesh.Ok).IsTrue();
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }

    [Test]
    public async Task Clone_And_PreviewNodes_ViaSession()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-cad-clone-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var settings = new CadEditorSettings(root);
            var session = new CadDocumentSession(settings);
            session.NewDocument();
            var box = new CadEntity
            {
                Kind = "box",
                Center = [0f, 0f, 0f],
                HalfExtents = [0.25f, 0.25f, 0.25f],
            };
            var bus = new CadCommandBus(session);
            bus.Execute(new AddEntityCommand(box));
            var cad = new CadSessionService(session, settings, bus, new CadCommandDispatcher(session, bus, settings));

            var clone = cad.Execute(new CadCommandDto
            {
                ActionId = CadSessionActionIds.Clone,
                SourceId = box.Id,
                Counts = [3, 1, 1],
                Spacing = [1f, 0f, 0f],
                Realization = "instances",
            });
            await Assert.That(clone.Ok).IsTrue();

            var light = cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.AddLight });
            await Assert.That(light.Ok).IsTrue();
            await Assert.That(session.Document.Entities.Any(e => e.Kind == "light")).IsTrue();

            var eval = new CadModelEvaluator().Evaluate(session.Document);
            await Assert.That(eval.Instances.Count).IsGreaterThanOrEqualTo(3);
            await Assert.That(eval.Lights.Count).IsEqualTo(1);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }
}
