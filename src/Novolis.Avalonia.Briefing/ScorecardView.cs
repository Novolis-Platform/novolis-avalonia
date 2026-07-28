using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Novolis.Avalonia.Briefing;

/// <summary>Kind / hits / hook scorecard for briefing moments.</summary>
public sealed class ScorecardView : UserControl
{
    static readonly IBrush FilledBrush = new SolidColorBrush(Color.Parse("#d4a017"));
    static readonly IBrush EmptyBrush = new SolidColorBrush(Color.Parse("#555555"));
    static readonly IBrush HookBrush = new SolidColorBrush(Color.Parse("#c8c8c8"));

    readonly StackPanel _rows = new() { Spacing = 6 };
    readonly TextBlock _title = new()
    {
        FontWeight = FontWeight.SemiBold,
        FontSize = 14,
        Foreground = FilledBrush,
        Margin = new Thickness(0, 0, 0, 4),
    };

    /// <summary>Scorecard rows.</summary>
    public static readonly StyledProperty<IReadOnlyList<ScorecardRow>?> RowsProperty =
        AvaloniaProperty.Register<ScorecardView, IReadOnlyList<ScorecardRow>?>(nameof(Rows));

    /// <summary>Optional title (e.g. <c>Life moments 6/15</c>).</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ScorecardView, string?>(nameof(Title));

    /// <summary>Creates an empty scorecard.</summary>
    public ScorecardView()
    {
        Content = new StackPanel
        {
            Spacing = 0,
            Children = { _title, _rows },
        };
        RowsProperty.Changed.AddClassHandler<ScorecardView>((v, _) => v.Rebuild());
        TitleProperty.Changed.AddClassHandler<ScorecardView>((v, _) =>
        {
            v._title.Text = v.Title ?? string.Empty;
            v._title.IsVisible = !string.IsNullOrEmpty(v.Title);
        });
        _title.IsVisible = false;
    }

    /// <summary>Scorecard rows.</summary>
    public IReadOnlyList<ScorecardRow>? Rows
    {
        get => GetValue(RowsProperty);
        set => SetValue(RowsProperty, value);
    }

    /// <summary>Optional title.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Replaces rows.</summary>
    public void SetRows(IReadOnlyList<ScorecardRow> rows, string? title = null)
    {
        if (title is not null)
            Title = title;
        Rows = rows;
    }

    void Rebuild()
    {
        _rows.Children.Clear();
        if (Rows is null || Rows.Count == 0)
        {
            _rows.Children.Add(new TextBlock
            {
                Text = "Quiet run — which bill became less dangerous?",
                Opacity = 0.7,
                FontStyle = FontStyle.Italic,
            });
            return;
        }

        foreach (var row in Rows)
            _rows.Children.Add(BuildRow(row));
    }

    static Control BuildRow(ScorecardRow row)
    {
        var mark = new TextBlock
        {
            Text = row.Filled ? "●" : "○",
            Foreground = row.Filled ? FilledBrush : EmptyBrush,
            Width = 18,
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var kind = new TextBlock
        {
            Text = row.Kind,
            FontWeight = FontWeight.SemiBold,
            Width = 140,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var hits = new TextBlock
        {
            Text = $"×{row.Hits}",
            Width = 36,
            Opacity = 0.8,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var hook = new TextBlock
        {
            Text = row.Hook,
            Foreground = HookBrush,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { mark, kind, hits, hook },
        };
    }
}
