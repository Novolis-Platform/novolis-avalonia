using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.GraphicalProfile;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using ProfileBinding = Novolis.Avalonia.GraphicalProfile.GraphicalProfileBinding;

namespace Novolis.Avalonia.Controls;

/// <summary>
/// Displays essential transfer information first and expandable metadata
/// such as hashes, alternate names, sources, pieces, and output paths.
/// </summary>
public sealed class DownloadDetailsView : Border
{
    private readonly StackPanel _root = new() { Spacing = GraphicalProfileColors.ControlGap };
    private DownloadItem? _item;
    private bool _showSummary = true;

    /// <summary>Creates an empty download detail surface.</summary>
    public DownloadDetailsView()
    {
        Padding = new Thickness(GraphicalProfileColors.CardPadding);
        BorderThickness = new Thickness(GraphicalProfileColors.Stroke);
        ProfileBinding.Bind(this, BackgroundProperty, Profile.BackgroundResourceKey);
        ProfileBinding.Bind(this, BorderBrushProperty, Profile.BorderResourceKey);
        Child = _root;
    }

    /// <summary>The snapshot currently displayed by the control.</summary>
    public DownloadItem? Item => _item;

    /// <summary>
    /// Whether the compact status summary is shown above the metadata section.
    /// Set this to <see langword="false"/> when embedding the view below a
    /// queue row.
    /// </summary>
    public bool ShowSummary
    {
        get => _showSummary;
        set
        {
            if (_showSummary == value)
            {
                return;
            }

            _showSummary = value;
            Rebuild();
        }
    }

    /// <summary>Raised when the host should perform an action for the item.</summary>
    public event Action<DownloadItem, DownloadCommand>? CommandRequested;

    /// <summary>Replaces the displayed item.</summary>
    public void SetItem(DownloadItem? item)
    {
        _item = item;
        Rebuild();
    }

    /// <summary>Rebuilds the visual content from the current item.</summary>
    public void Refresh() => Rebuild();

    private void Rebuild()
    {
        _root.Children.Clear();
        if (_item is not { } item)
        {
            _root.Children.Add(CreateText("Select a download to inspect its metadata.", Profile.MutedResourceKey));
            return;
        }

        if (_showSummary)
        {
            _root.Children.Add(BuildHeading(item));
            _root.Children.Add(BuildSummary(item));
        }

        _root.Children.Add(BuildMetadata(item));
        _root.Children.Add(BuildActions(item));
    }

    private static Control BuildHeading(DownloadItem item)
    {
        var heading = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = GraphicalProfileColors.ControlGap,
        };
        var title = CreateText(item.Name, Profile.TextResourceKey);
        title.FontSize = GraphicalProfileColors.IntervalTitleSize;
        title.FontWeight = FontWeight.Bold;
        title.TextTrimming = TextTrimming.CharacterEllipsis;

        var hash = DownloadDisplayFormatter.ValueOrPlaceholder(item.Metadata.Hash);
        if (hash.Length > 18)
        {
            hash = $"{hash[..18]}…";
        }

        var hashText = CreateText(hash, Profile.MutedResourceKey);
        hashText.FontFamily = Profile.MonoFont;
        hashText.TextTrimming = TextTrimming.CharacterEllipsis;
        Grid.SetColumn(hashText, 1);
        heading.Children.Add(title);
        heading.Children.Add(hashText);
        return heading;
    }

    private static Control BuildSummary(DownloadItem item)
    {
        var state = CreateText(
            FormatState(item),
            Profile.TextResourceKey);
        state.Foreground = StateBrush(item.State);
        state.FontWeight = FontWeight.SemiBold;

        return BuildFields(
            ("Status", state, false),
            ("Progress", DownloadDisplayFormatter.FormatProgress(item), false),
            ("Size", DownloadDisplayFormatter.FormatSizePair(item.CompletedBytes, item.TotalBytes), false),
            ("Availability", DownloadDisplayFormatter.FormatAvailability(item.Metadata), false),
            ("Seeds / peers", DownloadDisplayFormatter.FormatPeers(item.Metadata), false),
            ("Download / upload", $"{DownloadDisplayFormatter.FormatRate(item.DownloadBytesPerSecond)} / {DownloadDisplayFormatter.FormatRate(item.UploadBytesPerSecond)}", false),
            ("ETA", DownloadDisplayFormatter.FormatEta(item.Eta), false),
            ("Output", DownloadDisplayFormatter.ValueOrPlaceholder(item.Metadata.OutputLocation), false));
    }

    private static Control BuildMetadata(DownloadItem item)
    {
        var fields = BuildFields(
            ("Content type", item.Metadata.ContentType, false),
            ("Protocol", item.Metadata.Protocol, false),
            ("Hash / infohash", item.Metadata.Hash, true),
            ($"Alternate names ({item.Metadata.AlternateNames.Count:N0})", DownloadDisplayFormatter.FormatList(item.Metadata.AlternateNames), false),
            ("Source count", item.Metadata.SourceCount?.ToString("N0") ?? "—", false),
            ("Known sources", DownloadDisplayFormatter.FormatList(item.Metadata.Sources), false),
            ("Transfer path", item.Metadata.TransferPath, false),
            ("Output location", item.Metadata.OutputLocation, false),
            ("Pieces", FormatPieces(item.Metadata), false),
            ("Added", DownloadDisplayFormatter.FormatDate(item.Metadata.AddedAtUtc), false),
            ("Completed", DownloadDisplayFormatter.FormatDate(item.Metadata.CompletedAtUtc), false),
            ("Verification", item.Metadata.VerificationStatus, false),
            ("Error", item.Metadata.Error, false));

        var content = new StackPanel { Spacing = GraphicalProfileColors.ControlGap };
        content.Children.Add(fields);
        if (item.Metadata.Trackers.Count > 0)
        {
            content.Children.Add(CreateText("Trackers and discovery", Profile.MutedResourceKey));
            var trackerGrid = new DownloadTrackerGrid();
            trackerGrid.SetTrackers(item.Metadata.Trackers);
            content.Children.Add(trackerGrid);
        }

        if (item.Metadata.Files.Count > 0)
        {
            content.Children.Add(CreateText("Files", Profile.MutedResourceKey));
            var fileGrid = new DownloadFileGrid();
            fileGrid.SetFiles(item.Metadata.Files);
            content.Children.Add(fileGrid);
        }

        if (item.Metadata.Peers.Count > 0)
        {
            content.Children.Add(CreateText("Peer details", Profile.MutedResourceKey));
            var peerGrid = new DownloadPeerGrid();
            peerGrid.SetPeers(item.Metadata.Peers);
            content.Children.Add(peerGrid);
        }

        var expander = new Expander
        {
            Header = CreateText("More metadata, files, trackers, and sources", Profile.TextResourceKey),
            Content = content,
            IsExpanded = false,
            Margin = new Thickness(0, GraphicalProfileColors.ControlGap, 0, 0),
        };
        return expander;
    }

    private Control BuildActions(DownloadItem item)
    {
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = GraphicalProfileColors.ControlGap,
            Margin = new Thickness(0, GraphicalProfileColors.ControlGap, 0, 0),
        };

        switch (item.State)
        {
            case DownloadTransferState.Downloading:
            case DownloadTransferState.Checking:
            case DownloadTransferState.Seeding:
                actions.Children.Add(CreateActionButton(item, DownloadCommand.Pause, "Pause"));
                break;
            case DownloadTransferState.Failed:
                actions.Children.Add(CreateActionButton(item, DownloadCommand.Retry, "Retry"));
                break;
            case DownloadTransferState.Queued:
            case DownloadTransferState.Paused:
            case DownloadTransferState.Stalled:
                actions.Children.Add(CreateActionButton(item, DownloadCommand.Resume, "Resume"));
                break;
        }

        if (item.State is not DownloadTransferState.Completed
            and not DownloadTransferState.Removed)
        {
            actions.Children.Add(CreateActionButton(item, DownloadCommand.Cancel, "Cancel"));
        }

        if (!string.IsNullOrWhiteSpace(item.Metadata.Hash))
        {
            actions.Children.Add(CreateActionButton(item, DownloadCommand.Verify, "Verify"));
        }

        if (!string.IsNullOrWhiteSpace(item.Metadata.OutputLocation))
        {
            actions.Children.Add(CreateActionButton(item, DownloadCommand.OpenOutput, "Open output"));
        }

        return actions;
    }

    private Button CreateActionButton(DownloadItem item, DownloadCommand command, string label)
    {
        var button = new Button
        {
            Content = label,
            MinHeight = GraphicalProfileColors.TouchTarget,
            Padding = new Thickness(14, 8),
            Background = Profile.ActionBrush,
            Foreground = Profile.OnActionBrush,
        };
        button.Classes.Add("action-button");
        button.Click += (_, _) => CommandRequested?.Invoke(item, command);
        return button;
    }

    private static Grid BuildFields(params (string Label, object? Value, bool Mono)[] fields)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            ColumnSpacing = GraphicalProfileColors.SectionGap,
            RowSpacing = GraphicalProfileColors.ControlGap,
        };

        foreach (var (label, value, mono) in fields)
        {
            var row = grid.RowDefinitions.Count;
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var labelText = CreateText(label, Profile.MutedResourceKey);
            labelText.FontSize = GraphicalProfileColors.MetaSize;
            var valueText = value is TextBlock existing
                ? existing
                : CreateText(
                    DownloadDisplayFormatter.ValueOrPlaceholder(value?.ToString()),
                    Profile.TextResourceKey);
            valueText.TextWrapping = TextWrapping.Wrap;
            if (mono)
            {
                valueText.FontFamily = Profile.MonoFont;
            }

            Grid.SetRow(labelText, row);
            Grid.SetColumn(labelText, 0);
            Grid.SetRow(valueText, row);
            Grid.SetColumn(valueText, 1);
            grid.Children.Add(labelText);
            grid.Children.Add(valueText);
        }

        return grid;
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

    private static string FormatState(DownloadItem item)
    {
        return string.IsNullOrWhiteSpace(item.StatusMessage)
            ? item.State.ToString()
            : $"{item.State}: {item.StatusMessage}";
    }

    private static string FormatPieces(DownloadMetadata metadata)
    {
        if (metadata.PieceCount is null && metadata.PieceSizeBytes is null)
        {
            return "—";
        }

        return $"{metadata.PieceCount?.ToString("N0") ?? "?"} × {DownloadDisplayFormatter.FormatBytes(metadata.PieceSizeBytes)}";
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
