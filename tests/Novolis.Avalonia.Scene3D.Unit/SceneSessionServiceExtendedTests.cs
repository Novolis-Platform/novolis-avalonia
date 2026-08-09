using Novolis.Agent.Core;
using Novolis.Avalonia._3D.Session;
using Novolis._3D;

namespace Novolis.Avalonia.Unit.Scene3D;

public sealed class SceneSessionServiceExtendedTests
{
    [Test]
    public async Task Hello_Snapshot_Actions_MatchContract()
    {
        var session = new SceneSessionService(SceneDocument.CreateEmpty("Contract")) { AppId = "test-app" };
        var hello = session.Hello();
        await Assert.That(hello.AppId).IsEqualTo("test-app");
        await Assert.That(hello.SurfaceId).IsEqualTo("scene");

        var snap = session.Snapshot();
        await Assert.That(snap.DocumentName).IsEqualTo("Contract");
        await Assert.That(snap.Actions.Any(a => a.Id == SceneSessionActionIds.AddMesh)).IsTrue();

        var delete = session.Actions().Actions.First(a => a.Id == SceneSessionActionIds.Delete);
        await Assert.That(delete.Enabled).IsFalse();
        await Assert.That(delete.DisabledReason).IsEqualTo("noSelection");
    }

    [Test]
    public async Task New_Open_Save_RoundTrip()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-scene-io-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var session = new SceneSessionService(SceneDocument.CreatePrimitiveStage("SaveMe"));
            var path = Path.Combine(root, "stage.nov3djson");

            await Assert.That(session.Execute(new AgentCommand { ActionId = SceneSessionActionIds.Save, Path = path }).Ok).IsTrue();
            await Assert.That(File.Exists(path)).IsTrue();

            session.Execute(new AgentCommand { ActionId = SceneSessionActionIds.New });
            await Assert.That(session.Document.Name).IsNotEqualTo("SaveMe");

            await Assert.That(session.Execute(new AgentCommand { ActionId = SceneSessionActionIds.Open, Path = path }).Ok).IsTrue();
            await Assert.That(session.Document.Name).IsEqualTo("SaveMe");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }

    [Test]
    public async Task AddMesh_AddLight_SetTransform_Delete()
    {
        var session = new SceneSessionService(SceneDocument.CreateEmpty());
        var mesh = session.Execute(new AgentCommand
        {
            ActionId = SceneSessionActionIds.AddMesh,
            Primitive = "sphere",
            Name = "Ball",
        });
        await Assert.That(mesh.Ok).IsTrue();
        var node = session.Document.Nodes.OfType<MeshNode>().First(m => m.Name == "Ball");
        session.Document.SelectionId = node.Id;

        await Assert.That(session.Execute(new AgentCommand
        {
            ActionId = SceneSessionActionIds.SetTransform,
            NodeId = node.Id.ToString(),
            X = 1f,
            Y = 2f,
            Z = 3f,
        }).Ok).IsTrue();
        await Assert.That(node.Transform.Position[0]).IsEqualTo(1f);

        await Assert.That(session.Execute(new AgentCommand
        {
            ActionId = SceneSessionActionIds.AddLight,
            LightKind = "omni",
            Name = "Key",
        }).Ok).IsTrue();

        session.Document.SelectionId = node.Id;
        await Assert.That(session.Execute(new AgentCommand { ActionId = SceneSessionActionIds.Delete }).Ok).IsTrue();
        await Assert.That(session.Document.Nodes.OfType<MeshNode>().Any(m => m.Id == node.Id)).IsFalse();
    }

    [Test]
    public async Task DescribeScene_And_GroundPhrase()
    {
        var session = new SceneSessionService(SceneDocument.CreatePrimitiveStage("Describe"));
        var describe = session.Execute(new AgentCommand { ActionId = SceneSessionActionIds.DescribeScene });
        await Assert.That(describe.Ok).IsTrue();
        await Assert.That(describe.Message.Contains("Describe", StringComparison.Ordinal)).IsTrue();

        var box = session.Document.Nodes.OfType<MeshNode>().First(m => m.Primitive == MeshPrimitiveKind.Box);
        var ground = session.Execute(new AgentCommand
        {
            ActionId = SceneSessionActionIds.GroundPhrase,
            Phrase = box.Name,
            Select = true,
        });
        await Assert.That(ground.Ok).IsTrue();
        await Assert.That(session.Document.SelectionId).IsEqualTo(box.Id);
    }

    [Test]
    public async Task SetSceneProps_And_Continue()
    {
        var session = new SceneSessionService(SceneDocument.CreateEmpty());
        await Assert.That(session.Execute(new AgentCommand
        {
            ActionId = SceneSessionActionIds.SetSceneProps,
            Key = "author",
            Value = "unit-test",
        }).Ok).IsTrue();
        await Assert.That(session.Document.Properties).IsNotNull();
        await Assert.That(session.Document.Properties!["author"]).IsEqualTo("unit-test");

        await Assert.That(session.Continue().Ok).IsTrue();
    }

    [Test]
    public async Task DumpActions_RaiseHostEvents()
    {
        var session = new SceneSessionService(SceneDocument.CreateEmpty());
        string? dumpKind = null;
        var fit = false;
        session.DumpArtifactsRequested += k => dumpKind = k;
        session.FitRequested += () => fit = true;

        await Assert.That(session.Execute(new AgentCommand { ActionId = SceneSessionActionIds.DumpViewport }).Ok).IsTrue();
        await Assert.That(dumpKind).IsEqualTo("viewport");

        await Assert.That(session.Execute(new AgentCommand { ActionId = SceneSessionActionIds.Fit }).Ok).IsTrue();
        await Assert.That(fit).IsTrue();
    }

    [Test]
    public async Task Subscribe_RaisesActionResult()
    {
        var session = new SceneSessionService(SceneDocument.CreateEmpty());
        session.Subscribe();
        AgentActionResultEvent? evt = null;
        session.ActionResult += e => evt = e;
        session.Execute(new AgentCommand { ActionId = SceneSessionActionIds.New });
        await Assert.That(evt).IsNotNull();
        await Assert.That(evt!.Ok).IsTrue();
    }

    [Test]
    public async Task AddBoole_AddGenerator_SetEditMode_ViaSession()
    {
        var session = new SceneSessionService(SceneDocument.CreateBooleCut());
        var target = session.Document.Nodes.OfType<MeshNode>().First(m => m.Name.Contains("Target", StringComparison.OrdinalIgnoreCase));
        var cutter = session.Document.Nodes.OfType<MeshNode>().First(m => m.Name.Contains("Cutter", StringComparison.OrdinalIgnoreCase));

        var boole = session.Execute(new AgentCommand
        {
            ActionId = SceneSessionActionIds.AddBoole,
            BooleanKind = "difference",
            TargetId = target.Id.ToString(),
            CutterId = cutter.Id.ToString(),
        });
        await Assert.That(boole.Ok).IsTrue();

        session.Document.SelectionId = target.Id;
        await Assert.That(session.Execute(new AgentCommand
        {
            ActionId = SceneSessionActionIds.SetEditMode,
            EditMode = "edge",
        }).Ok).IsTrue();
        await Assert.That(session.Document.Edit.Mode).IsEqualTo(SceneEditMode.Edge);
    }

    [Test]
    public async Task ImportTriangles_FromInlineSoup()
    {
        var session = new SceneSessionService(SceneDocument.CreateEmpty());
        var result = session.Execute(new AgentCommand
        {
            ActionId = SceneSessionActionIds.ImportTriangles,
            Vertices = "0,0,0,1,0,0,0,1,0",
            Indices = "0,1,2",
            Name = "Tri",
        });
        await Assert.That(result.Ok).IsTrue();
        await Assert.That(session.Document.Nodes.OfType<MeshNode>().Any(m => m.Name == "Tri")).IsTrue();
    }
}
