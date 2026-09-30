using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.IO.Git;

namespace Novolis.Avalonia.Git;

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
