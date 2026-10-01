<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-avalonia/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-avalonia/) · [Source](https://github.com/Novolis-Platform/novolis-avalonia)
<!-- novolis-pkg-brand:end -->

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

## Quick start

```csharp
var chapters = new ChapterListPane();
var metadata = new MetadataFormPane();
var diagnostics = new DiagnosticsListPane();
```
