using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Novolis.Avalonia.Briefing;

/// <summary>Scrollable radio-style feed of <see cref="FeedLine"/> rows.</summary>
public sealed class FeedPanel : UserControl
{
    static readonly IBrush VoiceBrush = new SolidColorBrush(Color.Parse("#7eb8c9"));
    static readonly IBrush TextBrush = new SolidColorBrush(Color.Parse("#e6e6e6"));
    static readonly IBrush TagBrush = new SolidColorBrush(Color.Parse("#a89060"));

    readonly StackPanel _rows = new() { Spacing = 4 };
    readonly ScrollViewer _scroll;

    /// <summary>Feed lines to display.</summary>
    public static readonly StyledProperty<IReadOnlyList<FeedLine>?> LinesProperty =
        AvaloniaProperty.Register<FeedPanel, IReadOnlyList<FeedLine>?>(nameof(Lines));

    /// <summary>Creates an empty feed panel.</summary>
    public FeedPanel()
    {
        _scroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = _rows,
        };
        Content = _scroll;
        LinesProperty.Changed.AddClassHandler<FeedPanel>((panel, _) => panel.Rebuild());
    }

    /// <summary>Feed lines to display.</summary>
    public IReadOnlyList<FeedLine>? Lines
    {
        get => GetValue(LinesProperty);
        set => SetValue(LinesProperty, value);
    }

    /// <summary>Replaces the feed and scrolls to the end.</summary>
    public void SetLines(IReadOnlyList<FeedLine> lines)
    {
        Lines = lines;
    }

    /// <summary>Appends a line without clearing prior content.</summary>
    public void Append(FeedLine line)
    {
        var existing = Lines?.ToList() ?? [];
        existing.Add(line);
        Lines = existing;
        _scroll.ScrollToEnd();
    }

    void Rebuild()
    {
        _rows.Children.Clear();
        if (Lines is null)
            return;

        foreach (var line in Lines)
            _rows.Children.Add(BuildRow(line));

        _scroll.ScrollToEnd();
    }

    static Control BuildRow(FeedLine line)
    {
        var panel = new WrapPanel { Orientation = Orientation.Horizontal };
        if (!string.IsNullOrEmpty(line.Tag))
        {
            panel.Children.Add(new TextBlock
            {
                Text = line.Tag + " ",
                Foreground = TagBrush,
                FontFamily = new FontFamily("Consolas, Cascadia Mono, monospace"),
                FontSize = 12,
            });
        }

        panel.Children.Add(new TextBlock
        {
            Text = $"[{line.Voice}] ",
            Foreground = VoiceBrush,
            FontFamily = new FontFamily("Consolas, Cascadia Mono, monospace"),
            FontSize = 12,
        });
        panel.Children.Add(new TextBlock
        {
            Text = line.Text,
            Foreground = TextBrush,
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
        });
        return panel;
    }
}
