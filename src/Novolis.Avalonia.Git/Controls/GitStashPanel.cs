using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.IO.Git;

namespace Novolis.Avalonia.Git;

/// <summary>Stash list panel.</summary>
public sealed class GitStashPanel : UserControl
{
    readonly ListBox _list = new() { SelectionMode = SelectionMode.Single };
    readonly Button _apply = new() { Content = "Apply", IsEnabled = false };
    readonly Button _pop = new() { Content = "Pop", IsEnabled = false };
    readonly Button _drop = new()
    {
        Content = "Drop",
        IsEnabled = false,
        Foreground = Brushes.White,
        Background = new SolidColorBrush(Color.FromRgb(140, 48, 56)),
    };

    /// <summary>Stash command.</summary>
    public event EventHandler<GitChromeCommandEventArgs>? CommandRequested;

    /// <summary>Creates panel.</summary>
    public GitStashPanel()
    {
        ToolTip.SetTip(_apply, "Apply stash to the working tree (keeps the stash entry).");
        ToolTip.SetTip(_pop, "Apply stash, then remove it from the stash list.");
        ToolTip.SetTip(_drop, "Permanently delete the selected stash. Requires confirmation.");
        _apply.Click += (_, _) => Raise(GitChromeCommand.StashApply);
        _pop.Click += (_, _) => Raise(GitChromeCommand.StashPop);
        _drop.Click += (_, _) => Raise(GitChromeCommand.StashDrop);
        _list.SelectionChanged += (_, _) => UpdateButtons();
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
            UpdateButtons();
            return;
        }

        GitChromeUi.BindTextList(_list, static (StashRow r) => r.ToString());
        _list.ItemsSource = stashes.Select(s => new StashRow(s)).ToList();
        UpdateButtons();
    }

    void UpdateButtons()
    {
        var has = _list.SelectedItem is StashRow;
        _apply.IsEnabled = has;
        _pop.IsEnabled = has;
        _drop.IsEnabled = has;
    }

    void Raise(GitChromeCommand cmd)
    {
        if (_list.SelectedItem is not StashRow row)
            return;
        CommandRequested?.Invoke(
            this,
            new GitChromeCommandEventArgs(
                cmd,
                stashIndex: row.Entry.Index,
                detail: row.Entry.Message));
    }

    sealed class StashRow
    {
        public StashRow(StashEntry e) => Entry = e;
        public StashEntry Entry { get; }
        public override string ToString() => $"stash@{{{Entry.Index}}}  {Entry.Message}";
    }
}
