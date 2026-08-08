using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
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
            if (_tree.SelectedItem is TreeViewItem { Tag: TipRef tip })
                RefActivated?.Invoke(this, new GitRefActivatedEventArgs(tip));
        };
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        _tree.HorizontalAlignment = HorizontalAlignment.Stretch;
        _tree.VerticalAlignment = VerticalAlignment.Stretch;
        Content = _tree;
        ShowPlaceholder("Select a repository.");
    }

    /// <summary>Binds branch list.</summary>
    public void SetBranches(BranchList branches)
    {
        ArgumentNullException.ThrowIfNull(branches);
        // TreeView must own TreeViewItem instances via Items — ItemsSource of TreeViewItem
        // (or nested TipNode) leaves the pane blank under Avalonia 11.
        var local = new TreeViewItem { Header = $"LOCAL ({branches.Local.Count})", IsExpanded = true };
        foreach (var t in branches.Local)
        {
            local.Items.Add(new TreeViewItem
            {
                Header = t.Name == branches.Current ? $"● {t.Name}" : t.Name,
                Tag = t,
            });
        }

        var remote = new TreeViewItem { Header = $"REMOTE ({branches.Remote.Count})", IsExpanded = false };
        foreach (var t in branches.Remote)
            remote.Items.Add(new TreeViewItem { Header = t.Name, Tag = t });

        var tags = new TreeViewItem { Header = $"TAGS ({branches.Tags.Count})", IsExpanded = false };
        foreach (var t in branches.Tags)
            tags.Items.Add(new TreeViewItem { Header = t.Name, Tag = t });

        _tree.Items.Clear();
        _tree.Items.Add(local);
        _tree.Items.Add(remote);
        _tree.Items.Add(tags);
    }

    /// <summary>Shows a single placeholder row.</summary>
    public void ShowPlaceholder(string message)
    {
        _tree.Items.Clear();
        _tree.Items.Add(new TreeViewItem { Header = message, IsEnabled = false });
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
        GitChromeUi.BindTextList(_list, static (StashRow r) => r.ToString());
        var bar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(4),
            Children = { _apply, _pop, _drop },
        };
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        var host = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(bar, Dock.Bottom);
        host.Children.Add(bar);
        host.Children.Add(_list);
        Content = host;
        GitChromeUi.BindTextList(_list, static (string s) => s);
        _list.ItemsSource = new[] { "(no stashes)" };
    }

    /// <summary>Binds stashes.</summary>
    public void SetStashes(IReadOnlyList<StashEntry> stashes)
    {
        if (stashes.Count == 0)
        {
            GitChromeUi.BindTextList(_list, static (string s) => s);
            _list.ItemsSource = new[] { "(no stashes)" };
            return;
        }

        GitChromeUi.BindTextList(_list, static (StashRow r) => r.ToString());
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
    readonly TextBlock _body = new()
    {
        TextWrapping = TextWrapping.Wrap,
        Foreground = Brushes.WhiteSmoke,
    };

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
        Foreground = Brushes.WhiteSmoke,
        Background = new SolidColorBrush(Color.FromRgb(22, 24, 28)),
    };

    /// <summary>Creates view.</summary>
    public GitDiffView() => Content = _box;

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
    public GitWorkingTreeView()
    {
        GitChromeUi.BindTextList(_list, static (string s) => s);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Content = _list;
        ShowPlaceholder("Select a repository.");
    }

    /// <summary>Shows a placeholder row.</summary>
    public void ShowPlaceholder(string message) => _list.ItemsSource = new[] { message };

    /// <summary>Binds working tree.</summary>
    public void SetWorkingTree(WorkingTreeStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        var items = new List<string>();
        items.AddRange(status.Staged.Select(e => $"staged   {e.StatusCode}  {e.Path}"));
        items.AddRange(status.Unstaged.Select(e => $"unstaged {e.StatusCode}  {e.Path}"));
        items.AddRange(status.Untracked.Select(e => $"untracked     {e.Path}"));
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
            var b = new Button { Content = label, MinWidth = 72 };
            var c = cmd;
            b.Click += (_, _) => CommandRequested?.Invoke(this, new GitChromeCommandEventArgs(c));
            panel.Children.Add(b);
        }

        Content = panel;
    }
}

/// <summary>Shared chrome list styling so rows stay readable on dark shells.</summary>
internal static class GitChromeUi
{
    public static void BindTextList<T>(ListBox list, Func<T, string> text)
    {
        list.ItemTemplate = new FuncDataTemplate<T>((item, _) =>
            new TextBlock
            {
                Text = text(item),
                Foreground = Brushes.WhiteSmoke,
                Margin = new Thickness(6, 2),
                TextWrapping = TextWrapping.NoWrap,
            },
            supportsRecycling: true);
    }
}
