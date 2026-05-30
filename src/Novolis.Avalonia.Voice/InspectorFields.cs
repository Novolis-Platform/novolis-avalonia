using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Novolis.Avalonia.Voice;

internal static class InspectorFields
{
    public static TextBlock Header(string text) =>
        new()
        {
            Text = text,
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(0, 8, 0, 4),
        };

    public static StackPanel Labeled(string label, Control input)
    {
        var panel = new StackPanel { Spacing = 2, Margin = new Thickness(0, 0, 0, 6) };
        panel.Children.Add(new TextBlock { Text = label, Opacity = 0.85, FontSize = 12 });
        panel.Children.Add(input);
        return panel;
    }

    public static Slider CreateSlider(double min, double max, double value, double step, Action<double> onChanged)
    {
        var slider = new Slider
        {
            Minimum = min,
            Maximum = max,
            Value = value,
            SmallChange = step,
            LargeChange = step * 5,
        };
        slider.PropertyChanged += (_, e) =>
        {
            if (e.Property.Name == nameof(Slider.Value))
                onChanged(slider.Value);
        };
        return slider;
    }

    public static StackPanel SliderRow(
        string label,
        double value,
        double min,
        double max,
        double step,
        Action<double> apply,
        Action notify)
    {
        var valueText = new TextBlock { Opacity = 0.75, FontSize = 11 };
        var slider = CreateSlider(min, max, value, step, v =>
        {
            apply(v);
            valueText.Text = FormatValue(label, v);
            notify();
        });
        valueText.Text = FormatValue(label, slider.Value);
        var panel = new StackPanel { Spacing = 2, Margin = new Thickness(0, 0, 0, 8) };
        panel.Children.Add(new TextBlock { Text = label, Opacity = 0.85, FontSize = 12 });
        panel.Children.Add(slider);
        panel.Children.Add(valueText);
        return panel;
    }

    private static string FormatValue(string label, double value) =>
        label.Contains("Hz", StringComparison.OrdinalIgnoreCase)
            ? $"{value:0} Hz"
            : label.Contains("dB", StringComparison.OrdinalIgnoreCase)
                ? $"{value:0.##} dB"
                : $"{value:0.###}";
}
