using Novolis.Avalonia.Cad.Commands;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Scene;
using Novolis.Avalonia.Cad.Services;
using Novolis.Avalonia.Cad.Session;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Unit.Cad;

public sealed class CadSceneGraphTests
{
    [Test]
    public async Task BuildTree_NestsByParentId()
    {
        var parent = new CadEntity { Id = Guid.NewGuid(), Kind = "group", Name = "Root" };
        var child = new CadEntity { Id = Guid.NewGuid(), Kind = "box", Name = "Child", ParentId = parent.Id };
        var doc = new CadDocument();
        doc.Entities.AddRange([parent, child]);

        var tree = CadSceneGraph.BuildTree(doc);
        await Assert.That(tree.Count).IsEqualTo(1);
        await Assert.That(tree[0].Children.Count).IsEqualTo(1);
        await Assert.That(tree[0].Children[0].Name).IsEqualTo("Child");
    }

    [Test]
    public async Task Classify_CoversModifierAndMaterialKinds()
    {
        await Assert.That(CadSceneGraph.Classify(new CadEntity { Kind = "weld" }))
            .IsEqualTo(CadSceneNodeCategory.MeshModifier);
        await Assert.That(CadSceneGraph.Classify(new CadEntity { Kind = "material" }))
            .IsEqualTo(CadSceneNodeCategory.Material);
        await Assert.That(CadSceneGraph.Classify(new CadEntity { Kind = "camera" }))
            .IsEqualTo(CadSceneNodeCategory.Camera);
        await Assert.That(CadSceneGraph.Classify(new CadEntity { Kind = "unknownKind" }))
            .IsEqualTo(CadSceneNodeCategory.Unknown);
    }

    [Test]
    public async Task IsVisibleInWorkspace_FiltersByWorkspace()
    {
        await Assert.That(CadSceneGraph.IsVisibleInWorkspace(CadSceneNodeCategory.Light, "light", CadWorkspace.Cad))
            .IsFalse();
        await Assert.That(CadSceneGraph.IsVisibleInWorkspace(CadSceneNodeCategory.Light, "light", CadWorkspace.Preview))
            .IsTrue();
        await Assert.That(CadSceneGraph.IsVisibleInWorkspace(CadSceneNodeCategory.MeshModifier, "weld", CadWorkspace.Modeling))
            .IsTrue();
    }

    [Test]
    public async Task Find_And_ChildrenOf_Work()
    {
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var doc = new CadDocument();
        doc.Entities.AddRange(
        [
            new CadEntity { Id = parentId, Kind = "group" },
            new CadEntity { Id = childId, Kind = "box", ParentId = parentId },
        ]);

        await Assert.That(CadSceneGraph.Find(doc, childId)?.Kind).IsEqualTo("box");
        await Assert.That(CadSceneGraph.ChildrenOf(doc, parentId).Count()).IsEqualTo(1);
    }

    [Test]
    public async Task BuildTree_InfersBooleanRole()
    {
        var boolean = new CadEntity { Kind = "boolean", Operation = "union" };
        var doc = new CadDocument();
        doc.Entities.Add(boolean);
        var tree = CadSceneGraph.BuildTree(doc);
        await Assert.That(tree[0].Role).IsEqualTo("union");
    }
}
