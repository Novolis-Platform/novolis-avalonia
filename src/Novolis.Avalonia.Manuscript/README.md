# Novolis.Avalonia.Manuscript

Composable Avalonia chrome panels for manuscript editors — **not** a product host.

Composition: Layout shell → Controls atoms → **these panels** → app wires session/jobs.
See [avalonia-composition-grain](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/avalonia-composition-grain.md).

## Types

| Type | Role |
|------|------|
| `ChapterListFormatting` | Label helpers |
| `ChapterListPane` | Bindable chapter list |
| `MetadataFormPane` | Metadata fields + Apply |
| `DiagnosticsListPane` | Read-only findings list |
| `BookSelection` / `ChapterRef` | Typed chrome contracts |

## Install

```powershell
dotnet add package Novolis.Avalonia.Manuscript
```
