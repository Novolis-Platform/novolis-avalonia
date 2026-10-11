using Novolis.Avalonia.AssetStudio;
using Novolis.Visual.Definitions;

namespace Novolis.Avalonia.AssetStudio.Unit;

public sealed class AssetStudioNodePortListTests
{
    [Test]
    public async Task SetRecipe_ProducesDeterministicNodeThenPortOrder()
    {
        var firstId = new NodeId(Guid.Parse("30000000-0000-0000-0000-000000000001"));
        var secondId = new NodeId(Guid.Parse("30000000-0000-0000-0000-000000000002"));
        var recipe = new VisualRecipe(
            "ordered",
            nodes:
            [
                VisualNode.Create(
                    secondId,
                    VisualBuiltInNodeTypes.ConstantScalar,
                    1),
                VisualNode.Create(
                    firstId,
                    VisualBuiltInNodeTypes.ConstantScalar,
                    1),
            ]);

        var list = new AssetStudioNodePortList();
        list.SetRecipe(recipe);

        await Assert.That(list.Entries).Count().IsEqualTo(4);
        await Assert.That(list.Entries[0].NodeId).IsEqualTo(firstId);
        await Assert.That(list.Entries[0].IsPort).IsFalse();
        await Assert.That(list.Entries[1].IsPort).IsTrue();
        await Assert.That(list.Entries[1].PortId!.Value.Value).IsEqualTo("value");
        await Assert.That(list.SelectedEntry).IsNotNull();
    }

    [Test]
    public async Task MoveSelection_Clamps_AndKeepsSelectedEntryAccessible()
    {
        var nodeId = new NodeId(Guid.Parse("40000000-0000-0000-0000-000000000001"));
        var recipe = new VisualRecipe(
            "navigation",
            nodes:
            [
                VisualNode.Create(
                    nodeId,
                    VisualBuiltInNodeTypes.ConstantScalar,
                    1),
            ]);
        var list = new AssetStudioNodePortList();
        list.SetRecipe(recipe);

        list.MoveSelection(100);
        await Assert.That(list.SelectedIndex).IsEqualTo(list.Entries.Count - 1);
        await Assert.That(list.SelectedEntry?.IsPort).IsTrue();

        list.MoveSelection(-100);
        await Assert.That(list.SelectedIndex).IsEqualTo(0);
        await Assert.That(list.SelectedEntry?.NodeId).IsEqualTo(nodeId);
    }
}
