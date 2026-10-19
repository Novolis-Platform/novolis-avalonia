using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.GraphicalProfile;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using ProfileBinding = Novolis.Avalonia.GraphicalProfile.GraphicalProfileBinding;

namespace Novolis.Avalonia.Controls;

/// <summary>
/// Displays a dense, touch-friendly download queue with expandable metadata.
/// </summary>
public sealed class DownloadQueueView : Border
{
    private const string GridColumns = "*,95,180,110,140,90,95,95,90";
    private readonly StackPanel _rows = new() { Spacing = 1 };
    private readonly TextBlock _empty = CreateText(
        "No downloads in the queue.",
        Profile.MutedResourceKey);
    private IReadOnlyList<DownloadItem> _items = [];

    /// <summary>Creates an empty download queue.</summary>
    public DownloadQueueView()
    {
        Padding = new Thickness(0);
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

    /// <summary>The queue snapshots currently displayed.</summary>
    public IReadOnlyList<DownloadItem> Items => _items;

    /// <summary>The most recently expanded item.</summary>
    public DownloadItem? SelectedItem { get; private set; }

    /// <summary>Raised when the host should perform an action for an item.</summary>
    public event Action<DownloadItem, DownloadCommand>? CommandRequested;

    /// <summary>Raised when a row is expanded.</summary>
    public event Action<DownloadItem>? ItemExpanded;

    /// <summary>Replaces all queue rows.</summary>
    public void SetItems(IEnumerable<DownloadItem>? items)
    {
        _items = items?.ToArray() ?? [];
        SelectedItem = null;
        Rebuild();
    }

    /// <summary>Refreshes row visuals from the current snapshots.</summary>
    public void Refresh() => Rebuild();

    private void Rebuild()
    {
        _rows.Children.Clear();
        if (_items.Count == 0)
        {
            _rows.Children.Add(_empty);
            return;
        }

        foreach (var item in _items)
        {
            var details = new DownloadDetailsView { ShowSummary = false };
            details.SetItem(item);
            details.CommandRequested += (download, command) =>
                CommandRequested?.Invoke(download, command);

            var expander = new Expander
            {
                Header = BuildCompactRow(item),
                Content = details,
                MinHeight = GraphicalProfileColors.TouchTarget,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            };
            expander.Expanded += (_, _) =>
            {
                SelectedItem = item;
                ItemExpanded?.Invoke(item);
            };

            var row = new Border
            {
                Child = expander,
                BorderThickness = new Thickness(0, 0, 0, GraphicalProfileColors.Stroke),
                Background = Profile.SurfaceBrush,
                BorderBrush = Profile.BorderBrush,
            };
            _rows.Children.Add(row);
        }
    }

    private static Control BuildHeader()
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions(GridColumns),
            MinWidth = 1100,
            Background = Profile.RaisedBrush,
        };
        var headers = new[]
        {
            "Name",
            "Size",
            "Progress",
            "Status",
            "Availability",
            "Seeds / peers",
            "Down",
            "Up",
            "ETA",
        };
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

    private static Control BuildCompactRow(DownloadItem item)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions(GridColumns),
            MinWidth = 1100,
            MinHeight = GraphicalProfileColors.TouchTarget,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var name = new StackPanel { Spacing = 2, Margin = new Thickness(6, 3) };
        var title = CreateText(item.Name, Profile.TextResourceKey);
        title.FontWeight = FontWeight.SemiBold;
        title.TextTrimming = TextTrimming.CharacterEllipsis;
        name.Children.Add(title);
        var type = DownloadDisplayFormatter.ValueOrPlaceholder(
            item.Metadata.ContentType ?? item.Metadata.Protocol);
        var typeText = CreateText(type, Profile.MutedResourceKey);
        typeText.FontSize = GraphicalProfileColors.CaptionSize;
        typeText.TextTrimming = TextTrimming.CharacterEllipsis;
        name.Children.Add(typeText);
        Grid.SetColumn(name, 0);
        grid.Children.Add(name);

        AddCell(
            grid,
            DownloadDisplayFormatter.FormatBytes(item.TotalBytes),
            1);
        var progress = BuildProgress(item);
        Grid.SetColumn(progress, 2);
        grid.Children.Add(progress);

        var state = CreateText(
            item.State.ToString(),
            Profile.TextResourceKey);
        state.Foreground = StateBrush(item.State);
        state.FontWeight = FontWeight.SemiBold;
        state.Margin = new Thickness(6, 0);
        state.VerticalAlignment = VerticalAlignment.Center;
        state.TextTrimming = TextTrimming.CharacterEllipsis;
        Grid.SetColumn(state, 3);
        grid.Children.Add(state);

        AddCell(
            grid,
            DownloadDisplayFormatter.FormatAvailability(item.Metadata),
            4);
        AddCell(
            grid,
            DownloadDisplayFormatter.FormatPeers(item.Metadata),
            5);
        AddCell(
            grid,
            DownloadDisplayFormatter.FormatRate(item.DownloadBytesPerSecond),
            6);
        AddCell(
            grid,
            DownloadDisplayFormatter.FormatRate(item.UploadBytesPerSecond),
            7);
        AddCell(
            grid,
            DownloadDisplayFormatter.FormatEta(item.Eta),
            8);
        return grid;
    }

    private static Control BuildProgress(DownloadItem item)
    {
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
            Value = item.ProgressFraction ?? 0,
            IsIndeterminate = item.ProgressFraction is null,
            Height = 8,
            MinHeight = 8,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var label = CreateText(
            DownloadDisplayFormatter.FormatProgress(item),
            Profile.TextResourceKey);
        label.FontSize = GraphicalProfileColors.CaptionSize;
        label.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(bar, 0);
        Grid.SetColumn(label, 1);
        progress.Children.Add(bar);
        progress.Children.Add(label);
        return progress;
    }

    private static void AddCell(Grid grid, string value, int column)
    {
        var cell = CreateText(value, Profile.TextResourceKey);
        cell.Margin = new Thickness(6, 0);
        cell.VerticalAlignment = VerticalAlignment.Center;
        cell.TextTrimming = TextTrimming.CharacterEllipsis;
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

    private static IBrush StateBrush(DownloadTransferState state)
    {
        return state switch
        {
            DownloadTransferState.Downloading => Profile.AccentBrush,
            DownloadTransferState.Completed => Profile.ActionSoftBrush,
            DownloadTransferState.Failed => Profile.DangerBrush,
            DownloadTransferState.Stalled => Profile.WarningBrush,
            _ => Profile.MutedBrush,
        };
    }
}
