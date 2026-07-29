using Novolis.Avalonia.Layout;

namespace Novolis.Avalonia.Unit.Layout;

public sealed class DetailTreeNodeTests
{
    [Test]
    public async Task Constructor_Defaults_Children_To_Empty()
    {
        var node = new DetailTreeNode("Root");

        await Assert.That(node.Title).IsEqualTo("Root");
        await Assert.That(node.Description).IsNull();
        await Assert.That(node.Children).IsEmpty();
    }

    [Test]
    public async Task Constructor_Preserves_Description_And_Children()
    {
        var child = new DetailTreeNode("Child", "detail");
        var root = new DetailTreeNode("Root", "summary", [child]);

        await Assert.That(root.Description).IsEqualTo("summary");
        await Assert.That(root.Children).Count().IsEqualTo(1);
        await Assert.That(root.Children[0].Title).IsEqualTo("Child");
    }
}
