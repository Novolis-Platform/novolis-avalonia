using Avalonia.Controls;
using Avalonia.Media;
using Novolis.Avalonia.GraphicalProfile;

namespace Novolis.Avalonia.Unit;

public sealed class GraphicalProfileTests
{
    [Test]
    [Arguments(nameof(GraphicalProfileColors.BackgroundDark), "#080D1C")]
    [Arguments(nameof(GraphicalProfileColors.BackgroundLight), "#F5F7FC")]
    [Arguments(nameof(GraphicalProfileColors.SurfaceDark), "#111B31")]
    [Arguments(nameof(GraphicalProfileColors.SurfaceLight), "#FFFFFF")]
    [Arguments(nameof(GraphicalProfileColors.RaisedDark), "#172440")]
    [Arguments(nameof(GraphicalProfileColors.RaisedLight), "#EEF3FF")]
    [Arguments(nameof(GraphicalProfileColors.BorderDark), "#263A60")]
    [Arguments(nameof(GraphicalProfileColors.BorderLight), "#D7E0F0")]
    [Arguments(nameof(GraphicalProfileColors.TextDark), "#F4F7FF")]
    [Arguments(nameof(GraphicalProfileColors.TextLight), "#17213A")]
    [Arguments(nameof(GraphicalProfileColors.MutedDark), "#9AAECD")]
    [Arguments(nameof(GraphicalProfileColors.MutedLight), "#5B6B86")]
    [Arguments(nameof(GraphicalProfileColors.AccentDark), "#2FDFFF")]
    [Arguments(nameof(GraphicalProfileColors.AccentLight), "#2FDFFF")]
    [Arguments(nameof(GraphicalProfileColors.AccentFillDark), "#258BFF")]
    [Arguments(nameof(GraphicalProfileColors.AccentFillLight), "#258BFF")]
    [Arguments(nameof(GraphicalProfileColors.OnAccentFillDark), "#FFFFFF")]
    [Arguments(nameof(GraphicalProfileColors.OnAccentFillLight), "#FFFFFF")]
    [Arguments(nameof(GraphicalProfileColors.ActionDark), "#914BFF")]
    [Arguments(nameof(GraphicalProfileColors.ActionLight), "#914BFF")]
    [Arguments(nameof(GraphicalProfileColors.OnActionDark), "#FFFFFF")]
    [Arguments(nameof(GraphicalProfileColors.OnActionLight), "#FFFFFF")]
    [Arguments(nameof(GraphicalProfileColors.ActionSoftDark), "#167C88")]
    [Arguments(nameof(GraphicalProfileColors.ActionSoftLight), "#167C88")]
    [Arguments(nameof(GraphicalProfileColors.WarningDark), "#F0C56A")]
    [Arguments(nameof(GraphicalProfileColors.WarningLight), "#8A5A12")]
    [Arguments(nameof(GraphicalProfileColors.DangerDark), "#FF8D8D")]
    [Arguments(nameof(GraphicalProfileColors.DangerLight), "#C43545")]
    public async Task RoleHex_MatchesGovernanceBundle(string fieldName, string expected)
    {
        var value = typeof(GraphicalProfileColors).GetField(fieldName)?.GetValue(null);
        await Assert.That(value).IsEqualTo(expected);
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

    [Test]
    public async Task Binding_ResolvesBrushKeysForBrushProperties()
    {
        var brushKey = GraphicalProfileBinding.ResolveKey(
            TextBlock.ForegroundProperty,
            Novolis.Avalonia.GraphicalProfile.GraphicalProfile.TextResourceKey);
        var rawKey = GraphicalProfileBinding.ResolveKey(
            TextBlock.FontSizeProperty,
            Novolis.Avalonia.GraphicalProfile.GraphicalProfile.TextResourceKey);
        var isBrush = typeof(IBrush).IsAssignableFrom(TextBlock.ForegroundProperty.PropertyType);
        await Assert.That(brushKey).IsEqualTo("Ngp.TextBrush");
        await Assert.That(rawKey).IsEqualTo("Ngp.Text");
        await Assert.That(isBrush).IsTrue();
    }
}
