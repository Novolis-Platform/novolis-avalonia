using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Novolis.Avalonia.Briefing;

/// <summary>Two labeled metric columns with an explicit never-summed caption.</summary>
public sealed class DualMetricStrip : UserControl
{
    static readonly IBrush AccentBrush = new SolidColorBrush(Color.Parse("#d4a017"));
    static readonly IBrush ValueBrush = new SolidColorBrush(Color.Parse("#f0f0f0"));
    static readonly IBrush CaptionBrush = new SolidColorBrush(Color.Parse("#a89060"));

    readonly TextBlock _leftLabel = MakeLabel();
    readonly TextBlock _leftValue = MakeValue();
    readonly TextBlock _rightLabel = MakeLabel();
    readonly TextBlock _rightValue = MakeValue();
    readonly TextBlock _caption = new()
    {
        Text = "Never summed",
        Foreground = CaptionBrush,
        FontSize = 11,
        FontStyle = FontStyle.Italic,
        HorizontalAlignment = HorizontalAlignment.Center,
        Margin = new Thickness(0, 8, 0, 0),
    };

    /// <summary>Left column label.</summary>
    public static readonly StyledProperty<string> LeftLabelProperty =
        AvaloniaProperty.Register<DualMetricStrip, string>(nameof(LeftLabel), "Left");

    /// <summary>Left column value.</summary>
    public static readonly StyledProperty<string> LeftValueProperty =
        AvaloniaProperty.Register<DualMetricStrip, string>(nameof(LeftValue), "—");

    /// <summary>Right column label.</summary>
    public static readonly StyledProperty<string> RightLabelProperty =
        AvaloniaProperty.Register<DualMetricStrip, string>(nameof(RightLabel), "Right");

    /// <summary>Right column value.</summary>
    public static readonly StyledProperty<string> RightValueProperty =
        AvaloniaProperty.Register<DualMetricStrip, string>(nameof(RightValue), "—");

    /// <summary>Caption under the pair (default: never summed).</summary>
    public static readonly StyledProperty<string> CaptionProperty =
        AvaloniaProperty.Register<DualMetricStrip, string>(nameof(Caption), "Never summed");

    /// <summary>Creates the strip.</summary>
    public DualMetricStrip()
    {
        var left = new Border
        {
            Padding = new Thickness(12),
            BorderBrush = new SolidColorBrush(Color.Parse("#3a3a48")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = new StackPanel
            {
                Spacing = 4,
                Children = { _leftLabel, _leftValue },
            },
        };
        var right = new Border
        {
            Padding = new Thickness(12),
            BorderBrush = new SolidColorBrush(Color.Parse("#3a3a48")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = new StackPanel
            {
                Spacing = 4,
                Children = { _rightLabel, _rightValue },
            },
        };
        var columns = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            ColumnSpacing = 12,
            Children = { left, right },
        };
        Grid.SetColumn(right, 1);

        Content = new StackPanel
        {
            Children = { columns, _caption },
        };

        LeftLabelProperty.Changed.AddClassHandler<DualMetricStrip>((s, _) => s._leftLabel.Text = s.LeftLabel);
        LeftValueProperty.Changed.AddClassHandler<DualMetricStrip>((s, _) => s._leftValue.Text = s.LeftValue);
        RightLabelProperty.Changed.AddClassHandler<DualMetricStrip>((s, _) => s._rightLabel.Text = s.RightLabel);
        RightValueProperty.Changed.AddClassHandler<DualMetricStrip>((s, _) => s._rightValue.Text = s.RightValue);
        CaptionProperty.Changed.AddClassHandler<DualMetricStrip>((s, _) => s._caption.Text = s.Caption);

        _leftLabel.Text = LeftLabel;
        _leftValue.Text = LeftValue;
        _rightLabel.Text = RightLabel;
        _rightValue.Text = RightValue;
    }

    /// <summary>Left column label.</summary>
    public string LeftLabel
    {
        get => GetValue(LeftLabelProperty);
        set => SetValue(LeftLabelProperty, value);
    }

    /// <summary>Left column value.</summary>
    public string LeftValue
    {
        get => GetValue(LeftValueProperty);
        set => SetValue(LeftValueProperty, value);
    }

    /// <summary>Right column label.</summary>
    public string RightLabel
    {
        get => GetValue(RightLabelProperty);
        set => SetValue(RightLabelProperty, value);
    }

    /// <summary>Right column value.</summary>
    public string RightValue
    {
        get => GetValue(RightValueProperty);
        set => SetValue(RightValueProperty, value);
    }

    /// <summary>Caption under the pair.</summary>
    public string Caption
    {
        get => GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    /// <summary>Sets both columns and caption.</summary>
    public void SetPair(string leftLabel, string leftValue, string rightLabel, string rightValue, string? caption = null)
    {
        LeftLabel = leftLabel;
        LeftValue = leftValue;
        RightLabel = rightLabel;
        RightValue = rightValue;
        if (caption is not null)
            Caption = caption;
    }

    static TextBlock MakeLabel() => new()
    {
        Foreground = AccentBrush,
        FontSize = 12,
        FontWeight = FontWeight.SemiBold,
    };

    static TextBlock MakeValue() => new()
    {
        Foreground = ValueBrush,
        FontSize = 20,
        FontWeight = FontWeight.Bold,
    };
}
