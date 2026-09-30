using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Agent.Core;
using Novolis.Agent.Surface;
using Novolis.Avalonia.ThreeD.Session;
using Novolis.ThreeD;

namespace Novolis.Avalonia.ThreeD.Ui;

internal static class Chrome
{
    public static Button Btn(string label, Action onClick) => MakeBtn(label, onClick, Color.FromRgb(32, 48, 62));

    public static Button PrimaryBtn(string label, Action onClick) =>
        MakeBtn(label, onClick, Color.FromRgb(28, 72, 78));

    private static Button MakeBtn(string label, Action onClick, Color background)
    {
        var b = new Button
        {
            Content = label,
            Padding = new Thickness(8, 4),
            Background = new SolidColorBrush(background),
            Foreground = Brushes.WhiteSmoke,
            FontSize = 12,
        };
        b.Click += (_, _) => onClick();
        return b;
    }

    /// <summary>Labeled cluster for toolbar sections (CAD-style group box).</summary>
    public static Control Group(string title, params Control[] children)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(children);

        var body = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
        };
        foreach (var child in children)
        {
            if (child is Layoutable layout)
                layout.Margin = new Thickness(0);
            body.Children.Add(child);
        }

        return new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(48, 68, 84)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            Background = new SolidColorBrush(Color.FromArgb(40, 18, 28, 36)),
            Margin = new Thickness(4, 3),
            Padding = new Thickness(8, 4, 8, 5),
            Child = new StackPanel
            {
                Spacing = 3,
                Children =
                {
                    new TextBlock
                    {
                        Text = title.ToUpperInvariant(),
                        FontSize = 9,
                        LetterSpacing = 0.6,
                        FontWeight = FontWeight.SemiBold,
                        Opacity = 0.72,
                        Foreground = new SolidColorBrush(Color.FromRgb(160, 190, 200)),
                    },
                    body,
                },
            },
        };
    }

    public static Border Sep() => new()
    {
        Width = 1,
        Background = new SolidColorBrush(Color.FromRgb(60, 80, 100)),
        Margin = new Thickness(4, 2),
    };

    public static TextBlock Label(string text) => new()
    {
        Text = text,
        FontSize = 12,
        TextWrapping = TextWrapping.Wrap,
        Foreground = Brushes.WhiteSmoke,
    };
}
