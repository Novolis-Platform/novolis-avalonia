using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Novolis.Avalonia.Gaming;

/// <summary>
/// Lightweight labeled metric row for game HUDs (week, credits, status).
/// Domain mapping stays in the app; this control only lays out text.
/// </summary>
public sealed class HudStrip : UserControl
{
    static readonly IBrush DefaultFg = new SolidColorBrush(Color.Parse("#c8d8e8"));
    static readonly IBrush DefaultMuted = new SolidColorBrush(Color.Parse("#7a93a8"));
    static readonly IBrush DefaultBg = new SolidColorBrush(Color.FromArgb(200, 12, 22, 36));

    readonly TextBlock _left = MakeText(DefaultFg, 14, FontWeight.SemiBold);
    readonly TextBlock _center = MakeText(DefaultFg, 14, FontWeight.Normal);
    readonly TextBlock _right = MakeText(DefaultMuted, 13, FontWeight.Normal);
    readonly Border _root;

    /// <summary>Left cluster (e.g. WEEK 13).</summary>
    public static readonly StyledProperty<string> LeftTextProperty =
        AvaloniaProperty.Register<HudStrip, string>(nameof(LeftText), string.Empty);

    /// <summary>Center cluster (e.g. credits / metrics).</summary>
    public static readonly StyledProperty<string> CenterTextProperty =
        AvaloniaProperty.Register<HudStrip, string>(nameof(CenterText), string.Empty);

    /// <summary>Right cluster (e.g. window / selection status).</summary>
    public static readonly StyledProperty<string> RightTextProperty =
        AvaloniaProperty.Register<HudStrip, string>(nameof(RightText), string.Empty);

    /// <summary>Creates an empty strip.</summary>
    public HudStrip()
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            Margin = new Thickness(12, 6),
        };
        row.Children.Add(_left);
        Grid.SetColumn(_center, 1);
        _center.HorizontalAlignment = HorizontalAlignment.Center;
        row.Children.Add(_center);
        Grid.SetColumn(_right, 2);
        row.Children.Add(_right);

        _root = new Border
        {
            Background = DefaultBg,
            Child = row,
        };
        Content = _root;

        LeftTextProperty.Changed.AddClassHandler<HudStrip>((s, _) => s._left.Text = s.LeftText);
        CenterTextProperty.Changed.AddClassHandler<HudStrip>((s, _) => s._center.Text = s.CenterText);
        RightTextProperty.Changed.AddClassHandler<HudStrip>((s, _) => s._right.Text = s.RightText);
    }

    /// <summary>Left cluster text.</summary>
    public string LeftText
    {
        get => GetValue(LeftTextProperty);
        set => SetValue(LeftTextProperty, value);
    }

    /// <summary>Center cluster text.</summary>
    public string CenterText
    {
        get => GetValue(CenterTextProperty);
        set => SetValue(CenterTextProperty, value);
    }

    /// <summary>Right cluster text.</summary>
    public string RightText
    {
        get => GetValue(RightTextProperty);
        set => SetValue(RightTextProperty, value);
    }

    /// <summary>Strip background brush.</summary>
    public IBrush? StripBackground
    {
        get => _root.Background;
        set => _root.Background = value;
    }

    /// <summary>Sets all three clusters at once.</summary>
    public void SetTexts(string left, string center, string right)
    {
        LeftText = left;
        CenterText = center;
        RightText = right;
    }

    static TextBlock MakeText(IBrush fg, double size, FontWeight weight) => new()
    {
        Foreground = fg,
        FontSize = size,
        FontWeight = weight,
        VerticalAlignment = VerticalAlignment.Center,
    };
}
