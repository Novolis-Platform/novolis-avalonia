using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Novolis.Avalonia.Controls;

/// <summary>Job queue list with selected log tail and Cancel / Open actions.</summary>
public sealed class JobQueuePanel : Border
{
    readonly ListBox _list = new() { MaxHeight = 200 };
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
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            Margin = new Thickness(0, 2)
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
        var detail = new TextBlock
        {
            Text = row.Detail ?? "",
            FontSize = 11,
            Opacity = 0.7,
            TextWrapping = TextWrapping.Wrap,
            MaxHeight = 36,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(title, 0);
        Grid.SetColumn(status, 1);
        Grid.SetRow(detail, 1);
        Grid.SetColumnSpan(detail, 2);
        grid.Children.Add(title);
        grid.Children.Add(status);
        grid.Children.Add(detail);
        return grid;
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
