using Novolis.Avalonia.Controls;

namespace Novolis.Avalonia.Unit.Controls;

public sealed class FilteredPickerLogicTests
{
    [Test]
    public async Task Filter_Contains_Display()
    {
        var items = new[] { "Sol", "Sirius", "Altair" };
        var filtered = FilteredPickerLogic.Filter(items, "si", FilteredPickerLogic.ContainsDisplay<string>(s => s));
        await Assert.That(filtered.ToArray()).IsEquivalentTo(["Sirius"]);
    }

    [Test]
    public async Task Filter_Empty_Query_Returns_All()
    {
        var items = new[] { "a", "b" };
        var filtered = FilteredPickerLogic.Filter(items, "  ", FilteredPickerLogic.ContainsDisplay<string>(s => s));
        await Assert.That(filtered.Count).IsEqualTo(2);
    }
}
