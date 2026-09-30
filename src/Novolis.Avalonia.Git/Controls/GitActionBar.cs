using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.IO.Git;

namespace Novolis.Avalonia.Git;

/// <summary>Primary SCM action bar.</summary>
public sealed class GitActionBar : UserControl
{
    /// <summary>Command requested.</summary>
    public event EventHandler<GitChromeCommandEventArgs>? CommandRequested;

    /// <summary>Creates bar.</summary>
    public GitActionBar()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(8, 4) };
        foreach (var (label, cmd, tip) in new (string, GitChromeCommand, string)[]
                 {
                     ("Refresh", GitChromeCommand.Refresh, "Reload workspace status and the open repo."),
                     ("Fetch", GitChromeCommand.Fetch, "Fetch remotes (safe — does not change local branches)."),
                     ("Pull", GitChromeCommand.Pull, "Fast-forward pull only on selected repos. Confirms first."),
                     ("Push", GitChromeCommand.Push, "Push the open repo (never force). Confirms first."),
                     ("Branch", GitChromeCommand.CreateBranch, "Create a branch in the open repo."),
                     ("Branch cut", GitChromeCommand.BranchCut, "Plan a feature branch across selected repos. Dry-run + typed confirm."),
                     ("Stash", GitChromeCommand.StashPush, "Stash local changes in the open repo."),
                 })
        {
            var b = new Button { Content = label, MinWidth = 72 };
            var c = cmd;
            ToolTip.SetTip(b, tip);
            b.Click += (_, _) => CommandRequested?.Invoke(this, new GitChromeCommandEventArgs(c));
            panel.Children.Add(b);
        }

        Content = panel;
    }
}
