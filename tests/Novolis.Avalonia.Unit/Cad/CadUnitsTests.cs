using Novolis.Avalonia.Cad.Core;

namespace Novolis.Avalonia.Unit.Cad;

public sealed class CadUnitsTests
{
    [Test]
    public async Task Abbreviation_MapsKnownUnits()
    {
        await Assert.That(CadUnits.Abbreviation(CadUnits.Meter)).IsEqualTo("m");
        await Assert.That(CadUnits.Abbreviation(CadUnits.Centimeter)).IsEqualTo("cm");
        await Assert.That(CadUnits.Abbreviation(CadUnits.Millimeter)).IsEqualTo("mm");
        await Assert.That(CadUnits.Abbreviation(CadUnits.Inch)).IsEqualTo("in");
        await Assert.That(CadUnits.Abbreviation("unknown")).IsEqualTo("m");
    }

    [Test]
    public async Task ToDisplay_And_ToMeters_AreInverses()
    {
        const double meters = 2.5;
        await Assert.That(CadUnits.ToDisplay(meters, CadUnits.Centimeter)).IsEqualTo(250);
        await Assert.That(CadUnits.ToMeters(250, CadUnits.Centimeter)).IsEqualTo(meters);
        await Assert.That(CadUnits.ToDisplay(meters, CadUnits.Millimeter)).IsEqualTo(2500);
        await Assert.That(CadUnits.ToDisplay(meters, CadUnits.Inch)).IsGreaterThan(90);
    }

    [Test]
    public async Task FormatLength_IncludesUnit()
    {
        var text = CadUnits.FormatLength(1.234, CadUnits.Meter);
        await Assert.That(text.Contains("m", StringComparison.Ordinal)).IsTrue();
        await Assert.That(text.Contains("1.23", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task NiceScaleBar_PicksRoundLabel()
    {
        var (meters, label) = CadUnits.NiceScaleBar(0.01, CadUnits.Meter, targetPixels: 100);
        await Assert.That(meters).IsGreaterThan(0);
        await Assert.That(label.Contains("m", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Choices_ContainsAllStandardUnits()
    {
        await Assert.That(CadUnits.Choices).Contains(CadUnits.Meter);
        await Assert.That(CadUnits.Choices).Contains(CadUnits.Inch);
        await Assert.That(CadUnits.Choices.Length).IsEqualTo(4);
    }
}
