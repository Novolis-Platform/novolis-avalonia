using Novolis.Avalonia.Controls;

namespace Novolis.Avalonia.Unit.Controls;

public sealed class ChoiceDialogLogicTests
{
    [Test]
    public async Task ResolveDefault_Prefers_IsDefault()
    {
        var options = new[]
        {
            new ChoiceOption("a", "A"),
            new ChoiceOption("b", "B", IsDefault: true),
            new ChoiceOption("c", "C", IsCancel: true)
        };
        await Assert.That(ChoiceDialogLogic.ResolveDefault(options)!.Id).IsEqualTo("b");
        await Assert.That(ChoiceDialogLogic.ResolveCancel(options)!.Id).IsEqualTo("c");
    }

    [Test]
    public async Task ResolveDefault_Falls_Back_To_First()
    {
        var options = new[] { new ChoiceOption("keep", "Keep"), new ChoiceOption("reload", "Reload") };
        await Assert.That(ChoiceDialogLogic.ResolveDefault(options)!.Id).IsEqualTo("keep");
        await Assert.That(ChoiceDialogLogic.ResolveCancel(options)).IsNull();
    }
}
