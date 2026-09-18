<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
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
