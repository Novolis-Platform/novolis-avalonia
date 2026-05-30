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
}
