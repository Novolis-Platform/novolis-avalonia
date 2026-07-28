# Novolis.Avalonia.Briefing

Code-only Avalonia controls for post-run briefing UIs: radio-style feeds, scorecards, dual ledger strips, and metric tables.

## Install

```bash
dotnet add package Novolis.Avalonia.Briefing
```

## Controls

| Type | Use |
|------|-----|
| `FeedPanel` | Scrollable `[voice] text` rows |
| `ScorecardView` | Kind / hits / hook rows |
| `DualMetricStrip` | Two labeled metrics with a “never summed” caption |
| `MetricTableView` | Read-only DataGrid for keyed rows |

Domain mapping (campaign vox, registry standing, Ops vs Core) belongs in the app layer.
