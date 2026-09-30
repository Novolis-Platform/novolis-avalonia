using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.IO.Git;

namespace Novolis.Avalonia.Git;

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
