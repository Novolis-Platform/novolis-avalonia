using Novolis.Avalonia.Controls;

namespace Novolis.Avalonia.Unit.Controls;

public sealed class MarkedListRowTests
{
    [Test]
    public async Task CreateItem_Preserves_Fields()
    {
        var row = new MarkedListRow("*", "3", "Quiet Harbor", "420", Tag: 42);
        var control = MarkedListBox.CreateItem(row);
        await Assert.That(control).IsNotNull();
        await Assert.That(row.Primary).IsEqualTo("Quiet Harbor");
        await Assert.That(row.Tag).IsEqualTo(42);

        var list = MarkedListBox.Create([row]);
        await Assert.That(list.Items.Count).IsEqualTo(1);
    }
}
