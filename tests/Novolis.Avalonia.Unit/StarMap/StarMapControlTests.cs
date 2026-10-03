using Novolis.Avalonia.StarMap;
using Novolis.IO.Maps;

namespace Novolis.Avalonia.Unit.StarMap;

public sealed class StarMapControlTests
{
    [Test]
    public async Task Projected_scene_adapter_consumes_neutral_points_without_an_astro_reference()
    {
        var control = new StarMapControl();
        control.SetProjectedScene(
        [
            new ProjectedScenePoint("sol", 0, 0, "Sol", radiusPixels: 8),
            new ProjectedScenePoint("proxima", 4.2, -0.8, "Proxima Centauri"),
        ]);

        await Assert.That(control.Points).IsNotNull();
        await Assert.That(control.Points!).Count().IsEqualTo(2);
        await Assert.That(control.Points![0].Id).IsEqualTo("sol");
        await Assert.That(control.Points[0].Radius).IsEqualTo(8d);
        await Assert.That(control.Points[1].Label).IsEqualTo("Proxima Centauri");

        control.SelectedId = "sol";
        await Assert.That(control.SelectedProjectedPoint?.Id).IsEqualTo("sol");
        await Assert.That(control.SelectedProjectedPoint?.Label).IsEqualTo("Sol");

        control.Points = [];
        await Assert.That(control.SelectedProjectedPoint).IsNull();
    }
}
