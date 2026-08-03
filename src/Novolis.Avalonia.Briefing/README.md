<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Briefing

Code-only Avalonia controls for post-run briefing UIs: radio-style feeds, scorecards, dual ledger strips, and metric tables.

## Install

```bash
dotnet add package Novolis.Avalonia.Briefing
```

## Quick start

```csharp
using Novolis.Avalonia.Briefing;

var feed = new FeedPanel();
feed.Append(new FeedLine("vox", "berth clear"));

var scorecard = new ScorecardView();
var strip = new DualMetricStrip { LeftLabel = "Ops", RightLabel = "Core" };
var table = new MetricTableView();
```

## Controls

| Type | Use |
|------|-----|
| `FeedPanel` | Scrollable `[voice] text` rows |
| `ScorecardView` | Kind / hits / hook rows |
| `DualMetricStrip` | Two labeled metrics with a “never summed” caption |
| `MetricTableView` | Read-only DataGrid for keyed rows |

Domain mapping (campaign vox, registry standing, Ops vs Core) belongs in the app layer.

