using Avalonia.Media;
using Novolis.Avalonia.Map;

namespace Novolis.Avalonia.Unit.Map;

public sealed class MapInkTests
{
    [Test]
    public async Task Lerp_walks_from_dark_to_light()
    {
        var dark = Color.Parse("#010D18");
        var light = Color.Parse("#EFFDFF");

        var start = MapInk.Lerp(dark, light, 0);
        var middle = MapInk.Lerp(dark, light, 0.5);
        var end = MapInk.Lerp(dark, light, 1);

        await Assert.That(start).IsEqualTo(dark);
        await Assert.That(end).IsEqualTo(light);
        await Assert.That(middle.R).IsGreaterThan(start.R).And.IsLessThan(end.R);
        await Assert.That(middle.G).IsGreaterThan(start.G).And.IsLessThan(end.G);
        await Assert.That(middle.B).IsGreaterThan(start.B).And.IsLessThan(end.B);
    }
}
