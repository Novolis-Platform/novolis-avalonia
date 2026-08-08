using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.IO.Git;

namespace Novolis.Avalonia.Git;

/// <summary>Local / remote / tags navigator.</summary>
public sealed class GitBranchNavigator : UserControl
{
    readonly TreeView _tree = new();

    /// <summary>Ref activated (checkout request).</summary>
    public event EventHandler<GitRefActivatedEventArgs>? RefActivated;

    /// <summary>Creates navigator.</summary>
    public GitBranchNavigator()
    {
        _tree.DoubleTapped += (_, _) =>
        {
            if (_tree.SelectedItem is TipNode n)
                RefActivated?.Invoke(this, new GitRefActivatedEventArgs(n.Tip));
        };
        Content = _tree;
    }

    /// <summary>Binds branch list.</summary>
    public void SetBranches(BranchList branches)
    {
        ArgumentNullException.ThrowIfNull(branches);
        var local = new TreeViewItem
        {
            Header = $"LOCAL ({branches.Local.Count})",
            IsExpanded = true,
            ItemsSource = branches.Local.Select(t => new TipNode(t, t.Name == branches.Current)).ToList(),
        };
        var remote = new TreeViewItem
        {
            Header = $"REMOTE ({branches.Remote.Count})",
            IsExpanded = false,
            ItemsSource = branches.Remote.Select(t => new TipNode(t, false)).ToList(),
        };
        var tags = new TreeViewItem
        {
            Header = $"TAGS ({branches.Tags.Count})",
            IsExpanded = false,
            ItemsSource = branches.Tags.Select(t => new TipNode(t, false)).ToList(),
        };
        _tree.ItemsSource = new[] { local, remote, tags };
    }

    sealed class TipNode
    {
        public TipNode(TipRef tip, bool current)
        {
            Tip = tip;
            Current = current;
        }

        public TipRef Tip { get; }
        public bool Current { get; }
        public override string ToString() => Current ? $"● {Tip.Name}" : Tip.Name;
    }
}

/// <summary>Stash list panel.</summary>
public sealed class GitStashPanel : UserControl
{
    readonly ListBox _list = new() { SelectionMode = SelectionMode.Single };
    readonly Button _apply = new() { Content = "Apply" };
    readonly Button _pop = new() { Content = "Pop" };
    readonly Button _drop = new() { Content = "Drop" };

    /// <summary>Stash command.</summary>
    public event EventHandler<GitChromeCommandEventArgs>? CommandRequested;

    /// <summary>Creates panel.</summary>
    public GitStashPanel()
    {
        _apply.Click += (_, _) => Raise(GitChromeCommand.StashApply);
        _pop.Click += (_, _) => Raise(GitChromeCommand.StashPop);
        _drop.Click += (_, _) => Raise(GitChromeCommand.StashDrop);
        var bar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(4),
            Children = { _apply, _pop, _drop },
        };
        Content = new DockPanel
        {
            Children =
            {
                new TextBlock { Text = "Stashes", FontWeight = FontWeight.SemiBold, Margin = new Thickness(8, 8, 8, 4), [DockPanel.DockProperty] = Dock.Top },
                new Border { Child = bar, [DockPanel.DockProperty] = Dock.Bottom },
                _list,
            },
        };
    }

    /// <summary>Binds stashes.</summary>
    public void SetStashes(IReadOnlyList<StashEntry> stashes)
    {
        _list.ItemsSource = stashes.Select(s => new StashRow(s)).ToList();
    }

    void Raise(GitChromeCommand cmd)
    {
        var idx = (_list.SelectedItem as StashRow)?.Entry.Index;
        CommandRequested?.Invoke(this, new GitChromeCommandEventArgs(cmd, stashIndex: idx));
    }

    sealed class StashRow
    {
        public StashRow(StashEntry e) => Entry = e;
        public StashEntry Entry { get; }
        public override string ToString() => $"stash@{{{Entry.Index}}}  {Entry.Message}";
    }
}

/// <summary>Commit metadata pane.</summary>
public sealed class GitCommitDetailView : UserControl
{
    readonly TextBlock _body = new() { TextWrapping = TextWrapping.Wrap };

    /// <summary>Creates view.</summary>
    public GitCommitDetailView()
    {
        Content = new ScrollViewer { Content = _body, Margin = new Thickness(8) };
        _body.Text = "Select a commit.";
    }

    /// <summary>Binds detail.</summary>
    public void SetDetail(CommitDetail? detail)
    {
        if (detail is null)
        {
            _body.Text = "Select a commit.";
            return;
        }

        var c = detail.Commit;
        _body.Text =
            $"{c.Subject}\n\n{c.AuthorName} <{c.AuthorEmail}>\n{c.AuthorAt}\n{c.Sha}\n\n" +
            $"+{detail.FilesAdded}  -{detail.FilesDeleted}  ~{detail.FilesModified}\n\n" +
            string.Join('\n', detail.Paths.Take(40));
    }
}

/// <summary>Unified diff viewer.</summary>
public sealed class GitDiffView : UserControl
{
    readonly TextBox _box = new()
    {
        IsReadOnly = true,
        AcceptsReturn = true,
        TextWrapping = TextWrapping.NoWrap,
        FontFamily = new FontFamily("Cascadia Mono, Consolas, monospace"),
        FontSize = 12,
    };

    /// <summary>Creates view.</summary>
    public GitDiffView()
    {
        Content = _box;
    }

    /// <summary>Binds diff document.</summary>
    public void SetDiff(DiffDocument? doc)
    {
        if (doc is null || doc.Files.Count == 0)
        {
            _box.Text = "(no diff)";
            return;
        }

        var sb = new System.Text.StringBuilder();
        foreach (var f in doc.Files)
        {
            sb.AppendLine($"--- {f.OldPath ?? f.Path}");
            sb.AppendLine($"+++ {f.Path}");
            if (f.IsBinary)
            {
                sb.AppendLine("Binary file");
                continue;
            }

            foreach (var h in f.Hunks)
            {
                sb.AppendLine(h.Header);
                foreach (var line in h.Lines)
                    sb.Append(line.Kind).AppendLine(line.Text);
            }

            sb.AppendLine();
        }

        _box.Text = sb.ToString();
    }
}

/// <summary>Working tree groups.</summary>
public sealed class GitWorkingTreeView : UserControl
{
    readonly ListBox _list = new();

    /// <summary>Creates view.</summary>
    public GitWorkingTreeView() => Content = _list;

    /// <summary>Binds working tree.</summary>
    public void SetWorkingTree(WorkingTreeStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        var items = new List<string>();
        items.AddRange(status.Staged.Select(e => $"staged  {e.StatusCode}  {e.Path}"));
        items.AddRange(status.Unstaged.Select(e => $"unstaged {e.StatusCode}  {e.Path}"));
        items.AddRange(status.Untracked.Select(e => $"untracked {e.Path}"));
        _list.ItemsSource = items.Count == 0 ? ["(clean)"] : items;
    }
}

/// <summary>Primary SCM action bar.</summary>
public sealed class GitActionBar : UserControl
{
    /// <summary>Command requested.</summary>
    public event EventHandler<GitChromeCommandEventArgs>? CommandRequested;

    /// <summary>Creates bar.</summary>
    public GitActionBar()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(8, 4) };
        foreach (var (label, cmd) in new (string, GitChromeCommand)[]
                 {
                     ("Refresh", GitChromeCommand.Refresh),
                     ("Fetch", GitChromeCommand.Fetch),
                     ("Pull", GitChromeCommand.Pull),
                     ("Push", GitChromeCommand.Push),
                     ("Branch", GitChromeCommand.CreateBranch),
                     ("Branch cut", GitChromeCommand.BranchCut),
                     ("Stash", GitChromeCommand.StashPush),
                 })
        {
            var b = new Button { Content = label };
            var c = cmd;
            b.Click += (_, _) => CommandRequested?.Invoke(this, new GitChromeCommandEventArgs(c));
            panel.Children.Add(b);
        }

        Content = panel;
    }
}
