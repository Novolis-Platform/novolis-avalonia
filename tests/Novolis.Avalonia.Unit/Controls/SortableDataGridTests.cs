using Novolis.Avalonia.Controls;

namespace Novolis.Avalonia.Unit.Controls;

public sealed class SortableDataGridTests
{
    [Test]
    public async Task TextColumn_Configures_Header_And_Binding()
    {
        var column = SortableDataGrid.TextColumn("Port", "DestinationPort", 120);
        var binding = (global::Avalonia.Data.Binding)column.Binding!;

        await Assert.That(column.Header).IsEqualTo("Port");
        await Assert.That(binding.Path).IsEqualTo("DestinationPort");
        await Assert.That(column.IsReadOnly).IsTrue();
        await Assert.That(column.Width.Value).IsEqualTo(120);
    }

    [Test]
    public async Task TextColumn_DefaultWidth_IsNotExplicitlyFixed()
    {
        var column = SortableDataGrid.TextColumn("Protocol", "ProtocolName");

        await Assert.That(column.Width.IsAbsolute).IsFalse();
    }
}
