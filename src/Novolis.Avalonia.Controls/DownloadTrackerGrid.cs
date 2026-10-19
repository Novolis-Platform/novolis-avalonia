using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.GraphicalProfile;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using ProfileBinding = Novolis.Avalonia.GraphicalProfile.GraphicalProfileBinding;

namespace Novolis.Avalonia.Controls;

/// <summary>Touch-friendly tracker and source-discovery status grid.</summary>
public sealed class DownloadTrackerGrid : Border
{
    private const string GridColumns = "*,130,120,110,170,*";
    private readonly StackPanel _rows = new() { Spacing = 1 };
    private readonly TextBlock _empty = CreateText(
        "No tracker metadata is available.",
        Profile.MutedResourceKey);
    private IReadOnlyList<DownloadTrackerEntry> _trackers = [];

    /// <summary>Creates an empty tracker metadata grid.</summary>
    public DownloadTrackerGrid()
    {
        BorderThickness = new Thickness(GraphicalProfileColors.Stroke);
        ProfileBinding.Bind(this, BackgroundProperty, Profile.BackgroundResourceKey);
        ProfileBinding.Bind(this, BorderBrushProperty, Profile.BorderResourceKey);

        var content = new StackPanel { Spacing = GraphicalProfileColors.ControlGap };
        content.Children.Add(BuildHeader());
        content.Children.Add(_rows);
        Child = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = content,
        };
    }

    /// <summary>The tracker entries currently displayed.</summary>
    public IReadOnlyList<DownloadTrackerEntry> Trackers => _trackers;

    /// <summary>Replaces all tracker entries.</summary>
    public void SetTrackers(IEnumerable<DownloadTrackerEntry>? trackers)
    {
        _trackers = trackers?.ToArray() ?? [];
        Rebuild();
    }

    /// <summary>Refreshes row visuals from the current tracker entries.</summary>
    public void Refresh() => Rebuild();

    private void Rebuild()
    {
        _rows.Children.Clear();
        if (_trackers.Count == 0)
        {
            _rows.Children.Add(_empty);
            return;
        }

        foreach (var tracker in _trackers)
        {
            _rows.Children.Add(new Border
            {
                Child = BuildRow(tracker),
                MinHeight = GraphicalProfileColors.TouchTarget,
                Padding = new Thickness(6, 5),
                Background = Profile.SurfaceBrush,
                BorderBrush = Profile.BorderBrush,
                BorderThickness = new Thickness(0, 0, 0, GraphicalProfileColors.Stroke),
            });
        }
    }

    private static Control BuildHeader()
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions(GridColumns),
            MinWidth = 760,
            Background = Profile.RaisedBrush,
        };
        var headers = new[] { "Endpoint", "Protocol", "State", "Seeds / peers", "Last contact", "Error" };
        for (var index = 0; index < headers.Length; index++)
        {
            var header = CreateText(headers[index], Profile.MutedResourceKey);
            header.FontSize = GraphicalProfileColors.MetaSize;
            header.FontWeight = FontWeight.Bold;
            header.Margin = new Thickness(6, 8);
            Grid.SetColumn(header, index);
            grid.Children.Add(header);
        }

        return grid;
    }

    private static Control BuildRow(DownloadTrackerEntry tracker)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions(GridColumns),
            MinWidth = 760,
            MinHeight = GraphicalProfileColors.TouchTarget,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AddCell(grid, tracker.Endpoint, 0, mono: true);
        AddCell(grid, tracker.Protocol, 1);
        AddCell(grid, tracker.State, 2);
        var counts = $"{tracker.SeedCount?.ToString() ?? "—"} / {tracker.PeerCount?.ToString() ?? "—"}";
        AddCell(grid, counts, 3);
        AddCell(grid, DownloadDisplayFormatter.FormatDate(tracker.LastContactUtc), 4);
        AddCell(grid, tracker.Error, 5);
        return grid;
    }

    private static void AddCell(Grid grid, string? value, int column, bool mono = false)
    {
        var cell = CreateText(
            DownloadDisplayFormatter.ValueOrPlaceholder(value),
            Profile.TextResourceKey);
        cell.Margin = new Thickness(6, 0);
        cell.VerticalAlignment = VerticalAlignment.Center;
        cell.TextTrimming = TextTrimming.CharacterEllipsis;
        if (mono)
        {
            cell.FontFamily = Profile.MonoFont;
        }

        Grid.SetColumn(cell, column);
        grid.Children.Add(cell);
    }

    private static TextBlock CreateText(string text, string resourceKey)
    {
        var block = new TextBlock
        {
            Text = text,
            FontFamily = Profile.BodyFont,
        };
        ProfileBinding.Bind(block, TextBlock.ForegroundProperty, resourceKey);
        return block;
    }
}
