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
/// Touch-friendly peer/source grid with transfer rates, completion,
/// piece availability, and transferred totals.
/// </summary>
public sealed class DownloadPeerGrid : Border
{
    private const string GridColumns = "150,70,160,52,72,92,92,190,96,96,130";
    private readonly StackPanel _rows = new() { Spacing = 1 };
    private readonly TextBlock _empty = CreateText(
        "No peer details are available.",
        Profile.MutedResourceKey);
    private IReadOnlyList<DownloadPeer> _peers = [];

    /// <summary>Creates an empty peer/source grid.</summary>
    public DownloadPeerGrid()
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

    /// <summary>The peer snapshots currently displayed.</summary>
    public IReadOnlyList<DownloadPeer> Peers => _peers;

    /// <summary>The most recently selected peer row.</summary>
    public DownloadPeer? SelectedPeer { get; private set; }

    /// <summary>Raised when a peer row is clicked.</summary>
    public event Action<DownloadPeer>? PeerSelected;

    /// <summary>Replaces all rows in the grid.</summary>
    public void SetPeers(IEnumerable<DownloadPeer>? peers)
    {
        _peers = peers?.ToArray() ?? [];
        SelectedPeer = null;
        Rebuild();
    }

    /// <summary>Refreshes row visuals from the current peer snapshots.</summary>
    public void Refresh() => Rebuild();

    private void Rebuild()
    {
        _rows.Children.Clear();
        if (_peers.Count == 0)
        {
            _rows.Children.Add(_empty);
            return;
        }

        foreach (var peer in _peers)
        {
            var row = new Button
            {
                Content = BuildRow(peer),
                MinHeight = GraphicalProfileColors.TouchTarget,
                Padding = new Thickness(6, 5),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Background = Profile.SurfaceBrush,
                Foreground = Profile.TextBrush,
                BorderBrush = Profile.BorderBrush,
                BorderThickness = new Thickness(0, 0, 0, GraphicalProfileColors.Stroke),
            };
            row.Classes.Add("nav-button");
            row.Click += (_, _) =>
            {
                SelectedPeer = peer;
                PeerSelected?.Invoke(peer);
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
            Margin = new Thickness(0),
        };
        var headers = new[]
        {
            "Endpoint",
            "Location",
            "Client",
            "Dir",
            "Complete",
            "Down",
            "Up",
            "Pieces",
            "Uploaded",
            "Downloaded",
            "State",
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

    private static Control BuildRow(DownloadPeer peer)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions(GridColumns),
            MinWidth = 1100,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AddCell(grid, peer.Endpoint, 0, mono: true);
        AddCell(grid, peer.Location, 1);
        AddCell(grid, peer.Client, 2);
        AddCell(grid, peer.Direction, 3);
        AddCell(grid, DownloadDisplayFormatter.FormatPercent(peer.CompletionPercent), 4);
        AddCell(grid, DownloadDisplayFormatter.FormatRate(peer.DownloadBytesPerSecond), 5);
        AddCell(grid, DownloadDisplayFormatter.FormatRate(peer.UploadBytesPerSecond), 6);

        var pieceMap = new PeerPieceMap();
        pieceMap.SetPieces(peer.PieceAvailability);
        Grid.SetColumn(pieceMap, 7);
        grid.Children.Add(pieceMap);

        AddCell(grid, DownloadDisplayFormatter.FormatBytes(peer.UploadedBytes), 8);
        AddCell(grid, DownloadDisplayFormatter.FormatBytes(peer.DownloadedBytes), 9);
        AddCell(grid, peer.ConnectionState, 10);
        return grid;
    }

    private static void AddCell(
        Grid grid,
        string? value,
        int column,
        bool mono = false)
    {
        var text = CreateText(
            DownloadDisplayFormatter.ValueOrPlaceholder(value),
            Profile.TextResourceKey);
        text.VerticalAlignment = VerticalAlignment.Center;
        text.Margin = new Thickness(6, 0);
        text.TextTrimming = TextTrimming.CharacterEllipsis;
        if (mono)
        {
            text.FontFamily = Profile.MonoFont;
        }

        Grid.SetColumn(text, column);
        grid.Children.Add(text);
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
