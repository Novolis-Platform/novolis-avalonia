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
