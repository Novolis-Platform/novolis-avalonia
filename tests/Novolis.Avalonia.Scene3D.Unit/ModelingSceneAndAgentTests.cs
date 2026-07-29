using Novolis.Agent.Surface;
using Novolis.Avalonia._3D.Session;
using Novolis.Modeling.Scene;

namespace Novolis.Avalonia.Unit.Scene3D;

public sealed class ModelingSceneTests
{
    [Test]
    public async Task Empty_document_round_trips_json()
    {
        var doc = SceneDocument.CreatePrimitiveStage("RoundTrip");
        var json = SceneSerializer.Serialize(doc);
        var again = SceneSerializer.Deserialize(json);
        await Assert.That(again.Name).IsEqualTo("RoundTrip");
        await Assert.That(again.Nodes.OfType<MeshNode>().Any(m => m.Primitive == MeshPrimitiveKind.Torus)).IsTrue();
        await Assert.That(again.Format).IsEqualTo("novolis.scene");
    }

    [Test]
    public async Task Light_change_invalidates_look_only()
    {
        var doc = SceneDocument.CreatePrimitiveStage();
        var eval = new SceneEvaluator();
        eval.Bind(doc);
        var before = eval.Cache;
        var meshGen = before.MeshGeneration;
        var lookGen = before.LookGeneration;

        var light = doc.Nodes.OfType<LightNode>().First();
        light.Intensity = 9f;
        eval.NotifyNodeChanged(light);
        var after = eval.Cache;

        await Assert.That(after.MeshGeneration).IsEqualTo(meshGen);
        await Assert.That(after.LookGeneration).IsGreaterThan(lookGen);
        await Assert.That(after.Lights.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Primitive_mesher_builds_nonempty_meshes()
    {
        foreach (MeshPrimitiveKind kind in Enum.GetValues<MeshPrimitiveKind>())
        {
            var mesh = PrimitiveMesher.Tessellate(new MeshNode { Primitive = kind, Size = [1, 1, 1], Segments = 12 });
            await Assert.That(mesh.TriangleCount).IsGreaterThan(0);
        }
    }

    [Test]
    public async Task Cloner_expands_to_count_instances()
    {
        var doc = SceneDocument.CreateClonerRow();
        var eval = new SceneEvaluator();
        eval.Bind(doc);
        var meshes = eval.Cache.EvaluatedMeshes;
        // Floor remains + 5 cloner instances (source box consumed by cloner).
        await Assert.That(meshes.Count).IsEqualTo(6);
        await Assert.That(meshes.All(m => m.Indices.Length > 0)).IsTrue();
    }

    [Test]
    public async Task Boole_difference_produces_mesh()
    {
        var doc = SceneDocument.CreateBooleCut();
        var eval = new SceneEvaluator();
        eval.Bind(doc);
        var boole = eval.Cache.EvaluatedMeshes.FirstOrDefault(m =>
            doc.Find(m.SourceId) is GeneratorNode { Generator: GeneratorKind.Boole });
        await Assert.That(boole).IsNotNull();
        await Assert.That(boole!.Indices.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task Weld_modifier_reduces_or_keeps_vertices()
    {
        var doc = SceneDocument.CreatePrimitiveStage();
        var box = doc.Nodes.OfType<MeshNode>().First(m => m.Primitive == MeshPrimitiveKind.Box);
        doc.Nodes.Add(new ModifierNode
        {
            Name = "Weld",
            ParentId = doc.Roots().First().Id,
            Modifier = ModifierKind.Weld,
            InputId = box.Id,
            Tolerance = 0.01f,
        });
        var eval = new SceneEvaluator();
        eval.Bind(doc);
        var result = eval.Cache.EvaluatedMeshes.First(m => m.SourceId == box.Id);
        await Assert.That(result.Vertices.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task Extrude_moves_vertices()
    {
        var box = PrimitiveMesher.Box(1, 1, 1);
        var extruded = MeshShaping.Extrude(box, 0.5f);
        await Assert.That(extruded.VertexCount).IsEqualTo(box.VertexCount);
        var moved = false;
        for (var i = 0; i < box.VertexCount; i++)
        {
            if (box.Vertices[i] != extruded.Vertices[i])
                moved = true;
        }

        await Assert.That(moved).IsTrue();
    }

    [Test]
    public async Task Rendering_light_export_maps_kinds()
    {
        var doc = SceneDocument.CreateLookSetup();
        var eval = new SceneEvaluator();
        eval.Bind(doc);
        var exported = RenderingLightExport.Export(eval.Cache);
        await Assert.That(exported.Any(e => e.Kind == "Directional")).IsTrue();
        await Assert.That(exported.Any(e => e.Kind == "Point")).IsTrue();
    }

    [Test]
    public async Task Make_editable_bakes_vertices()
    {
        var session = new SceneSessionService(SceneDocument.CreatePrimitiveStage("Bake"));
        var box = session.Document.Nodes.OfType<MeshNode>().First(m => m.Primitive == MeshPrimitiveKind.Box);
        session.Document.SelectionId = box.Id;
        var result = session.Execute(new AgentCommandDto { ActionId = SceneSessionActionIds.MakeEditable });
        await Assert.That(result.Ok).IsTrue();
        await Assert.That(box.Vertices).IsNotNull();
        await Assert.That(box.Indices).IsNotNull();
        await Assert.That(box.Vertices!.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task Face_extrude_adds_triangles()
    {
        var box = PrimitiveMesher.Box(1, 1, 1);
        var before = box.TriangleCount;
        var extruded = MeshComponentOps.ExtrudeFaces(box, [0], 0.25f);
        await Assert.That(extruded.TriangleCount).IsGreaterThan(before);
    }

    [Test]
    public async Task Component_selection_and_move_via_session()
    {
        var session = new SceneSessionService(SceneDocument.CreateEditBox());
        var mesh = session.Document.Nodes.OfType<MeshNode>().First(m => m.Name.Contains("Editable"));
        var beforeY = mesh.Vertices![1];
        session.Execute(new AgentCommandDto
        {
            ActionId = SceneSessionActionIds.SetEditMode,
            EditMode = "point",
            NodeId = mesh.Id.ToString(),
        });
        session.Execute(new AgentCommandDto
        {
            ActionId = SceneSessionActionIds.SelectComponents,
            Indices = "0,1,2",
        });
        session.Execute(new AgentCommandDto
        {
            ActionId = SceneSessionActionIds.MoveSelection,
            Y = 0.5f,
        });
        await Assert.That(session.Document.Edit.SelectedVertices.Count).IsEqualTo(3);
        await Assert.That(mesh.Vertices![1]).IsNotEqualTo(beforeY);
    }

    [Test]
    public async Task Mesh_picker_hits_box_face()
    {
        var box = PrimitiveMesher.Box(2, 2, 2);
        var evaluated = EvaluatedMesh.FromEditable(Guid.NewGuid(), box, System.Numerics.Matrix4x4.Identity);
        var ray = new Ray(new System.Numerics.Vector3(0, 0, 5), new System.Numerics.Vector3(0, 0, -1));
        var hit = MeshPicker.Pick([evaluated], ray, SceneEditMode.Polygon);
        await Assert.That(hit).IsNotNull();
        await Assert.That(hit!.Value.Mode).IsEqualTo(SceneEditMode.Polygon);
    }
}

public sealed class AgentSurfaceDefinitionTests
{
    [Test]
    public async Task Scene_definition_includes_boole_and_primitives()
    {
        var def = SceneSessionContract.Definition;
        await Assert.That(def.SurfaceId).IsEqualTo("scene");
        await Assert.That(def.Actions.Any(a => a.Id == "addboole")).IsTrue();
        await Assert.That(def.Actions.Any(a => a.Id == "addmesh")).IsTrue();
        var discovery = def.ToDiscoveryJson();
        await Assert.That(discovery.Contains("addboole", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Scene_definition_includes_edit_actions()
    {
        var def = SceneSessionContract.Definition;
        await Assert.That(def.Actions.Any(a => a.Id == "seteditmode")).IsTrue();
        await Assert.That(def.Actions.Any(a => a.Id == "meshedit")).IsTrue();
        await Assert.That(def.Actions.Any(a => a.Id == "makeeditable")).IsTrue();
    }

    [Test]
    public async Task Http_host_addmesh_cylinder_and_addboole()
    {
        var session = new SceneSessionService(SceneDocument.CreatePrimitiveStage("HttpTest")) { AppId = "test" };
        var port = 18885 + Random.Shared.Next(0, 200);
        await using var host = AgentHttpHost.Attach(session, session.Definition, port);
        using var client = new HttpClient { BaseAddress = new Uri(host.BaseUrl + "/") };

        using (var content = new StringContent(
                   """{"actionId":"addmesh","primitive":"cylinder","name":"Cyl"}""",
                   System.Text.Encoding.UTF8,
                   "application/json"))
        {
            using var response = await client.PostAsync("session/command", content);
            response.EnsureSuccessStatusCode();
        }

        await Assert.That(session.Document.Nodes.OfType<MeshNode>().Any(m => m.Primitive == MeshPrimitiveKind.Cylinder && m.Name == "Cyl")).IsTrue();
    }
}
