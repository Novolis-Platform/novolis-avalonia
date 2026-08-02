using Avalonia.Controls;
using Novolis.Avalonia.Voice;

namespace Novolis.Avalonia.Unit.Voice;

public sealed class InspectorFieldsTests
{
    [Test]
    public async Task Header_SetsLabelText()
    {
        var header = InspectorFields.Header("Gain");
        await Assert.That(header.Text).IsEqualTo("Gain");
    }

    [Test]
    public async Task CreateSlider_InvokesCallbackOnValueChange()
    {
        double seen = 0;
        var slider = InspectorFields.CreateSlider(0, 10, 5, 0.5, v => seen = v);
        slider.Value = 7.5;
        await Assert.That(seen).IsEqualTo(7.5);
    }

    [Test]
    public async Task SliderRow_FormatsHzAndDbLabels()
    {
        var hzRow = InspectorFields.SliderRow("Cutoff Hz", 440, 20, 20000, 1, _ => { }, () => { });
        var dbRow = InspectorFields.SliderRow("Output dB", -6, -24, 6, 0.5, _ => { }, () => { });

        var hzValueText = AssertSliderRowValueText(hzRow);
        var dbValueText = AssertSliderRowValueText(dbRow);

        await Assert.That(hzValueText).IsEqualTo("440 Hz");
        await Assert.That(dbValueText).IsEqualTo("-6 dB");
    }

    private static string AssertSliderRowValueText(StackPanel row)
    {
        var valueText = row.Children.OfType<TextBlock>().Last();
        return valueText.Text ?? string.Empty;
    }
}
