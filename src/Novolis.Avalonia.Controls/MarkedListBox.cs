using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Novolis.Avalonia.Controls;

/// <summary>A four-column list row: marker, leading, primary, trailing (all strings).</summary>
/// <param name="Marker">Optional dirty / status glyph (e.g. "*").</param>
/// <param name="Leading">Short leading label (e.g. number).</param>
/// <param name="Primary">Main title text.</param>
/// <param name="Trailing">Trailing meta (e.g. count).</param>
/// <param name="Tag">Optional payload for selection handlers.</param>
public sealed record MarkedListRow(
    string? Marker,
    string? Leading,
    string Primary,
    string? Trailing,
    object? Tag = null);

/// <summary>Builds code-first list items for <see cref="MarkedListRow"/>.</summary>
public static class MarkedListBox
{
    /// <summary>Creates a control representing one marked row.</summary>
    public static Control CreateItem(MarkedListRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*,Auto"),
            Margin = new Thickness(0, 2)
        };

        var marker = new TextBlock
        {
            Text = row.Marker ?? "",
            Width = 12,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#E8A838"))
        };
        var leading = new TextBlock
        {
            Text = row.Leading ?? "",
            Width = 36,
            Opacity = 0.7,
            FontFamily = new FontFamily("Consolas, Courier New, monospace")
        };
        var primary = new TextBlock
        {
            Text = row.Primary,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(6, 0)
        };
        var trailing = new TextBlock
        {
            Text = row.Trailing ?? "",
            Opacity = 0.45,
            FontSize = 11
        };

        Grid.SetColumn(marker, 0);
        Grid.SetColumn(leading, 1);
        Grid.SetColumn(primary, 2);
        Grid.SetColumn(trailing, 3);
        grid.Children.Add(marker);
        grid.Children.Add(leading);
        grid.Children.Add(primary);
        grid.Children.Add(trailing);
        return grid;
    }

    /// <summary>Creates a <see cref="ListBox"/> populated with marked rows (Tag = row).</summary>
    public static ListBox Create(IEnumerable<MarkedListRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var list = new ListBox();
        foreach (var row in rows)
        {
            list.Items.Add(new ListBoxItem
            {
                Content = CreateItem(row),
                Tag = row
            });
        }

        return list;
    }
}
