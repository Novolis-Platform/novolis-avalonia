using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.GraphicalProfile;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using ProfileBinding = Novolis.Avalonia.GraphicalProfile.GraphicalProfileBinding;

namespace Novolis.Avalonia.Controls;

/// <summary>Touch-friendly file manifest and per-file progress grid.</summary>
public sealed class DownloadFileGrid : Border
{
    private const string GridColumns = "*,125,185,110,130";
    private readonly StackPanel _rows = new() { Spacing = 1 };
    private readonly TextBlock _empty = CreateText(
        "No file metadata is available.",
        Profile.MutedResourceKey);
    private IReadOnlyList<DownloadFileEntry> _files = [];

    /// <summary>Creates an empty file metadata grid.</summary>
    public DownloadFileGrid()
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

    /// <summary>The file entries currently displayed.</summary>
    public IReadOnlyList<DownloadFileEntry> Files => _files;

    /// <summary>Replaces all file entries.</summary>
    public void SetFiles(IEnumerable<DownloadFileEntry>? files)
    {
        _files = files?.ToArray() ?? [];
        Rebuild();
    }

    /// <summary>Refreshes row visuals from the current file entries.</summary>
    public void Refresh() => Rebuild();

    private void Rebuild()
    {
        _rows.Children.Clear();
        if (_files.Count == 0)
        {
            _rows.Children.Add(_empty);
            return;
        }

        foreach (var file in _files)
        {
            _rows.Children.Add(new Border
            {
                Child = BuildRow(file),
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
            MinWidth = 670,
            Background = Profile.RaisedBrush,
        };
        var headers = new[] { "Path", "Size", "Progress", "Priority", "Integrity" };
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

    private static Control BuildRow(DownloadFileEntry file)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions(GridColumns),
            MinWidth = 670,
            MinHeight = GraphicalProfileColors.TouchTarget,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AddCell(grid, file.Path, 0, mono: true);
        AddCell(grid, DownloadDisplayFormatter.FormatSizePair(file.CompletedBytes, file.SizeBytes), 1);

        var progress = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 6,
            Margin = new Thickness(6, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        var bar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 1,
            Value = file.ProgressFraction ?? 0,
            IsIndeterminate = file.ProgressFraction is null,
            Height = 8,
            MinHeight = 8,
        };
        var label = CreateText(
            DownloadDisplayFormatter.FormatPercent(
                file.ProgressFraction is { } fraction ? fraction * 100 : null),
            Profile.TextResourceKey);
        Grid.SetColumn(bar, 0);
        Grid.SetColumn(label, 1);
        progress.Children.Add(bar);
        progress.Children.Add(label);
        Grid.SetColumn(progress, 2);
        grid.Children.Add(progress);

        AddCell(grid, file.Priority, 3);
        AddCell(grid, file.IntegrityStatus, 4);
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
