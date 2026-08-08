using Novolis.Avalonia.Git;
using Novolis.IO.Git;

namespace Novolis.Avalonia.Unit;

public sealed class GitChromeTests
{
    [Test]
    public async Task Chrome_command_args_carry_stash_index()
    {
        var args = new GitChromeCommandEventArgs(
            GitChromeCommand.StashDrop,
            @"d:\novolis\novolis-io",
            stashIndex: 2,
            detail: "WIP on main");
        await Assert.That(args.Command).IsEqualTo(GitChromeCommand.StashDrop);
        await Assert.That(args.StashIndex).IsEqualTo(2);
        await Assert.That(args.RepoPath).IsEqualTo(@"d:\novolis\novolis-io");
        await Assert.That(args.Detail).IsEqualTo("WIP on main");
    }

    [Test]
    public async Task Confirm_request_defaults_to_warning()
    {
        var req = new GitConfirmRequest
        {
            Title = "Drop stash",
            Summary = "Permanently delete stash@{0}.",
            RequireTypedPhrase = "drop",
        };
        await Assert.That(req.Severity).IsEqualTo(GitConfirmSeverity.Warning);
        await Assert.That(req.RequireTypedPhrase).IsEqualTo("drop");
    }

    [Test]
    public async Task Repo_open_and_selection_event_args()
    {
        var repo = new RepoEntry { Name = "novolis-io", Path = @"d:\novolis\novolis-io", IsGit = true };
        var open = new RepoOpenEventArgs(repo);
        var sel = new RepoSelectionChangedEventArgs(new RepoSelection
        {
            Root = @"d:\novolis",
            Selected = [repo],
        });
        await Assert.That(open.Repo.Name).IsEqualTo("novolis-io");
        await Assert.That(sel.Selection.Selected.Count).IsEqualTo(1);
    }

    [Test]
    public async Task BranchCutDialog_preview_accepts_plan()
    {
        var plan = new BranchPlan
        {
            Id = "abc",
            Name = "feat/x",
            BaseRef = "main",
            WorkspaceRoot = @"d:\novolis",
            Steps =
            [
                new BranchCutRepoStep
                {
                    Repo = new RepoEntry { Name = "novolis-io", Path = @"d:\novolis\novolis-io", IsGit = true },
                    PlannedArgs = ["checkout", "-B", "feat/x", "main"],
                },
            ],
        };
        await Assert.That(plan.Steps.Count).IsEqualTo(1);
        await Assert.That(plan.Name).IsEqualTo("feat/x");
    }
}
