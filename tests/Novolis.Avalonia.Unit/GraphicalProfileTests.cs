using Novolis.Avalonia.GraphicalProfile;

namespace Novolis.Avalonia.Unit.GraphicalProfile;

public sealed class GraphicalProfileTests
{
    [Test]
    public async Task DarkPalette_MatchesGovernanceBundle()
    {
        await Assert.That(GraphicalProfileColors.BackgroundDark).IsEqualTo("#080D1C");
        await Assert.That(GraphicalProfileColors.SurfaceDark).IsEqualTo("#111B31");
        await Assert.That(GraphicalProfileColors.AccentDark).IsEqualTo("#2FDFFF");
        await Assert.That(GraphicalProfileColors.AccentFillDark).IsEqualTo("#258BFF");
        await Assert.That(GraphicalProfileColors.ActionDark).IsEqualTo("#914BFF");
    }

    [Test]
    public async Task LightPalette_MatchesGovernanceBundle()
    {
        await Assert.That(GraphicalProfileColors.BackgroundLight).IsEqualTo("#F5F7FC");
        await Assert.That(GraphicalProfileColors.SurfaceLight).IsEqualTo("#FFFFFF");
        await Assert.That(GraphicalProfileColors.RaisedLight).IsEqualTo("#EEF3FF");
        await Assert.That(GraphicalProfileColors.BorderLight).IsEqualTo("#D7E0F0");
    }

    [Test]
    public async Task Geometry_ProvidesTouchAndCardContracts()
    {
        var touchTarget = GraphicalProfileColors.TouchTarget;
        var iconControl = GraphicalProfileColors.IconControl;
        var cardRadius = GraphicalProfileColors.CardRadius;
        var primaryRadius = GraphicalProfileColors.PrimaryRadius;

        await Assert.That(touchTarget).IsEqualTo(42);
        await Assert.That(iconControl).IsEqualTo(44);
        await Assert.That(cardRadius).IsEqualTo(18);
        await Assert.That(primaryRadius).IsEqualTo(22);
    }
}
