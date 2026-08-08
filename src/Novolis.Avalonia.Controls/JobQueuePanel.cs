using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Novolis.Avalonia.Controls;

/// <summary>Job queue list with selected log tail, progress bars, and Cancel / Open actions.</summary>
public sealed class JobQueuePanel : Border
{
    readonly ListBox _list = new() { MinHeight = 120 };
    readonly TextBlock _log = new()
    {
        FontFamily = new FontFamily("Consolas, Courier New, monospace"),
        FontSize = 11,
        TextWrapping = TextWrapping.Wrap
    };
    readonly Button _cancel = new() { Content = "Cancel", Padding = new Thickness(6, 2), IsEnabled = false };
    readonly Button _open = new() { Content = "Open", Padding = new Thickness(6, 2), IsEnabled = false };
    readonly List<IJobQueueRow> _rows = [];

    /// <summary>Creates an empty job queue panel.</summary>
    public JobQueuePanel()
    {
        Padding = new Thickness(0);
        _list.SelectionChanged += (_, _) => SyncSelection();
        _cancel.Click += (_, _) => TryCancelSelected();
        _open.Click += (_, _) => TryOpenSelected();

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Margin = new Thickness(0, 4, 0, 0),
            Children = { _cancel, _open }
        };

        var logHost = new Border
        {
            Padding = new Thickness(6),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.Parse("#3F3F46")),
            Background = new SolidColorBrush(Color.Parse("#1E1E1E")),
            Child = _log,
            Margin = new Thickness(0, 8, 0, 0)
        };

        Child = new StackPanel
        {
            Spacing = 4,
            Children = { _list, actions, logHost }
        };
    }

    /// <summary>Currently selected row, if any.</summary>
    public IJobQueueRow? SelectedRow { get; private set; }

    /// <summary>Raised when Cancel is clicked for the selected row.</summary>
    public event Action<IJobQueueRow>? CancelRequested;

    /// <summary>Raised when Open is clicked for the selected row.</summary>
    public event Action<IJobQueueRow>? OpenOutputRequested;

    /// <summary>Replaces the job list.</summary>
    public void SetJobs(IEnumerable<IJobQueueRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        _rows.Clear();
        _rows.AddRange(rows);
        RebuildList();
    }

    /// <summary>Refreshes list item visuals from current row state.</summary>
    public void Refresh() => RebuildList(preserveSelection: true);

    void RebuildList(bool preserveSelection = false)
    {
        var selectedTag = preserveSelection ? SelectedRow?.Tag : null;
        _list.Items.Clear();
        foreach (var row in _rows)
        {
            var item = new ListBoxItem
            {
                Content = BuildRowContent(row),
                Tag = row
            };
            _list.Items.Add(item);
            if (selectedTag is not null && Equals(row.Tag, selectedTag))
                _list.SelectedItem = item;
        }

        if (_list.SelectedItem is null && _list.Items.Count > 0)
            _list.SelectedIndex = 0;
        SyncSelection();
    }

    static Control BuildRowContent(IJobQueueRow row)
    {
        var root = new StackPanel
        {
            Spacing = 4,
            Margin = new Thickness(0, 2)
        };

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };
        var title = new TextBlock
        {
            Text = row.Title,
            FontWeight = FontWeight.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var status = new TextBlock
        {
            Text = row.StatusLabel,
            Opacity = 0.8,
            Margin = new Thickness(8, 0, 0, 0)
        };
        Grid.SetColumn(title, 0);
        Grid.SetColumn(status, 1);
        header.Children.Add(title);
        header.Children.Add(status);
        root.Children.Add(header);

        if (!string.IsNullOrWhiteSpace(row.Detail))
        {
            root.Children.Add(new TextBlock
            {
                Text = row.Detail,
                FontSize = 11,
                Opacity = 0.7,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
        }

        if (row.Progress is { } progress)
        {
            var overallLabel = new TextBlock
            {
                Text = row.ProgressLabel ?? $"{progress:P0}",
                FontSize = 11,
                Opacity = 0.85
            };
            var overallBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 1,
                Value = Math.Clamp(progress, 0, 1),
                Height = 8,
                MinHeight = 8
            };
            root.Children.Add(overallLabel);
            root.Children.Add(overallBar);
        }

        if (row.StepProgress is { Count: > 0 } chapters)
        {
            var chapterHost = new StackPanel { Spacing = 3, Margin = new Thickness(0, 2, 0, 0) };
            foreach (var chapter in chapters)
            {
                var line = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                    RowDefinitions = new RowDefinitions("Auto,Auto")
                };
                var name = new TextBlock
                {
                    Text = chapter.Label,
                    FontSize = 11,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                var chapterStatus = new TextBlock
                {
                    Text = chapter.StatusLabel ?? "",
                    FontSize = 10,
                    Opacity = 0.75,
                    Margin = new Thickness(6, 0, 0, 0)
                };
                var bar = new ProgressBar
                {
                    Minimum = 0,
                    Maximum = 1,
                    Value = Math.Clamp(chapter.Progress, 0, 1),
                    Height = 5,
                    MinHeight = 5,
                    Margin = new Thickness(0, 1, 0, 0)
                };
                Grid.SetColumn(name, 0);
                Grid.SetColumn(chapterStatus, 1);
                Grid.SetRow(bar, 1);
                Grid.SetColumnSpan(bar, 2);
                line.Children.Add(name);
                line.Children.Add(chapterStatus);
                line.Children.Add(bar);
                chapterHost.Children.Add(line);
            }

            root.Children.Add(new ScrollViewer
            {
                MaxHeight = 220,
                Content = chapterHost
            });
        }

        return root;
    }

    /// <summary>Raises <see cref="CancelRequested"/> for the selected row when cancellable.</summary>
    public bool TryCancelSelected()
    {
        if (SelectedRow is not { CanCancel: true } row)
            return false;
        CancelRequested?.Invoke(row);
        return true;
    }

    /// <summary>Raises <see cref="OpenOutputRequested"/> for the selected row when allowed.</summary>
    public bool TryOpenSelected()
    {
        if (SelectedRow is not { CanOpenOutput: true } row)
            return false;
        OpenOutputRequested?.Invoke(row);
        return true;
    }

    void SyncSelection()
    {
        SelectedRow = _list.SelectedItem is ListBoxItem { Tag: IJobQueueRow row } ? row : null;
        _log.Text = SelectedRow?.LogTail ?? "";
        _cancel.IsEnabled = SelectedRow?.CanCancel == true;
        _open.IsEnabled = SelectedRow?.CanOpenOutput == true;
    }
}
