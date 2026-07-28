using Avalonia.Controls;
using Novolis.Avalonia.Studio;

namespace Novolis.Avalonia.Unit.Studio;

public sealed class StudioFocusModeTests
{
    [Test]
    public async Task Apply_Toggles_Visibility()
    {
        var a = new Border();
        var b = new TextBlock();
        StudioFocusMode.Apply(focused: true, a, b, null);
        await Assert.That(a.IsVisible).IsFalse();
        await Assert.That(b.IsVisible).IsFalse();
        StudioFocusMode.Apply(focused: false, a, b);
        await Assert.That(a.IsVisible).IsTrue();
        await Assert.That(b.IsVisible).IsTrue();
    }
}
