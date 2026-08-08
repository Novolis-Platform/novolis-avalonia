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

    /// <summary>Selection changed.</summary>
    public event EventHandler<RepoSelectionChangedEventArgs>? SelectionChanged;

    /// <summary>Open repo (double-click / Enter).</summary>
    public event EventHandler<RepoOpenEventArgs>? RepoOpenRequested;

    /// <summary>Creates the visualizer.</summary>
    public GitRepoVisualizer()
    {
        _list.SelectionChanged += (_, _) => RaiseSelection();
        _list.DoubleTapped += OnDoubleTapped;
        // Enter also opens — Windows Avalonia sometimes eats DoubleTapped on ListBox items.
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
        Content = new DockPanel
        {
            LastChildFill = true,
            Children =
            {
                new TextBlock
                {
                    Text = "Repositories (double-click to open)",
                    FontWeight = FontWeight.SemiBold,
                    Margin = new Thickness(8, 8, 8, 4),
                    [DockPanel.DockProperty] = Dock.Top,
                },
                _list,
            },
        };
    }

    /// <summary>Binds a status matrix.</summary>
    public void SetMatrix(WorkspaceStatusMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        _matrix = matrix;
        _list.ItemsSource = matrix.Repos.Select(r => new RepoRow(r)).ToList();
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
