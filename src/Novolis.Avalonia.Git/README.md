<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Git

Composable Avalonia chrome for Git: repo matrix, commit graph, branch navigator, stash, diff, action bar, and branch dialogs. Bound to [`Novolis.IO.Git`](https://github.com/Novolis-Platform/novolis-io) DTOs — **not** a product host (see RepoStudio).

## Install

```bash
dotnet add package Novolis.Avalonia.Git
```

## Controls

| Control | Role |
|---------|------|
| `GitRepoVisualizer` | Multi-select workspace matrix |
| `GitCommitGraphView` | Lane graph + commit list |
| `GitBranchNavigator` | Local / remote / tags |
| `GitStashPanel` | Stash list + apply/pop/drop |
| `GitCommitDetailView` / `GitDiffView` / `GitWorkingTreeView` | Detail panes |
| `GitActionBar` | Fetch / pull / push / branch / stash |
| `GitCreateBranchDialog` / `GitBranchCutDialog` | Dialog bodies |

Hosts wire events (`CommandRequested`, `RepoOpenRequested`, …) to `Novolis.IO.Git` APIs.
