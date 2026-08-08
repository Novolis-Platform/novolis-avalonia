using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.IO.Git;

namespace Novolis.Avalonia.Git;

/// <summary>Multi-repo status matrix with multi-select.</summary>
public sealed class GitRepoVisualizer : UserControl
{
    readonly ListBox _list = new()
    {
        SelectionMode = SelectionMode.Multiple,
    };

    WorkspaceStatusMatrix? _matrix;
    bool _suppressOpen;

    /// <summary>Selection changed.</summary>
    public event EventHandler<RepoSelectionChangedEventArgs>? SelectionChanged;

    /// <summary>Open repo (single-select / double-click / Enter).</summary>
    public event EventHandler<RepoOpenEventArgs>? RepoOpenRequested;

    /// <summary>Creates the visualizer.</summary>
    public GitRepoVisualizer()
    {
        GitChromeUi.BindTextList(_list, static (RepoRow r) => r.ToString());
        _list.SelectionChanged += OnSelectionChanged;
        _list.DoubleTapped += OnDoubleTapped;
        _list.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter && _list.SelectedItem is RepoRow row)
            {
                RepoOpenRequested?.Invoke(this, new RepoOpenEventArgs(row.Row.Repo));
                e.Handled = true;
            }
        };
        _list.HorizontalAlignment = HorizontalAlignment.Stretch;
        _list.VerticalAlignment = VerticalAlignment.Stretch;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Content = _list;
    }

    /// <summary>Binds a status matrix.</summary>
    public void SetMatrix(WorkspaceStatusMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        _matrix = matrix;
        var previous = GetSelection().Selected.Select(r => r.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _suppressOpen = true;
        try
        {
            var rows = matrix.Repos.Select(r => new RepoRow(r)).ToList();
            _list.ItemsSource = rows;
            foreach (var row in rows)
            {
                if (previous.Contains(row.Row.Repo.Path))
                    _list.SelectedItems?.Add(row);
            }
        }
        finally
        {
            _suppressOpen = false;
        }

        RaiseSelection();
    }

    /// <summary>Selects a repo by path without treating it as a multi-select batch.</summary>
    public void SelectRepo(string path)
    {
        if (_list.ItemsSource is not IEnumerable<RepoRow> rows)
            return;
        var match = rows.FirstOrDefault(r =>
            string.Equals(r.Row.Repo.Path, path, StringComparison.OrdinalIgnoreCase));
        if (match is null)
            return;
        _suppressOpen = true;
        try
        {
            _list.SelectedItems?.Clear();
            _list.SelectedItem = match;
        }
        finally
        {
            _suppressOpen = false;
        }
    }

    /// <summary>Current selection.</summary>
    public RepoSelection GetSelection()
    {
        var root = _matrix?.Root ?? "";
        var selected = _list.SelectedItems?
            .OfType<RepoRow>()
            .Select(r => r.Row.Repo)
            .ToArray() ?? [];
        return new RepoSelection { Root = root, Selected = selected };
    }

    void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RaiseSelection();
        if (_suppressOpen)
            return;
        // Single highlighted repo opens detail panes; multi-select is for batch fetch/pull.
        if (_list.SelectedItems?.Count == 1 && _list.SelectedItem is RepoRow row)
            RepoOpenRequested?.Invoke(this, new RepoOpenEventArgs(row.Row.Repo));
    }

    void RaiseSelection() =>
        SelectionChanged?.Invoke(this, new RepoSelectionChangedEventArgs(GetSelection()));

    void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_list.SelectedItem is RepoRow row)
            RepoOpenRequested?.Invoke(this, new RepoOpenEventArgs(row.Row.Repo));
    }

    sealed class RepoRow
    {
        public RepoRow(RepoStatusRow row) => Row = row;
        public RepoStatusRow Row { get; }
        public override string ToString()
        {
            var s = Row.Status;
            if (s is null)
                return $"{Row.Repo.Name}  ({Row.Error ?? "no status"})";
            var flags = new List<string>();
            if (s.Dirty) flags.Add("dirty");
            if (s.Behind > 0) flags.Add($"↓{s.Behind}");
            if (s.Ahead > 0) flags.Add($"↑{s.Ahead}");
            if (Row.StashCount > 0) flags.Add($"stash:{Row.StashCount}");
            var suffix = flags.Count == 0 ? "ok" : string.Join(' ', flags);
            return $"{Row.Repo.Name}  [{s.Branch}]  {suffix}";
        }
    }
}
