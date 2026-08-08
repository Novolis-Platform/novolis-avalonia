<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Layout

No-XAML shell layouts: WireShark-style `AnalyzerWorkspace` and adaptive `AuthoringWorkspace` (Wide / Narrow).

## Install

```bash
dotnet add package Novolis.Avalonia.Layout
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), Avalonia 11.

## Quick start — AuthoringWorkspace

Shared vocabulary for desktop (Wide: nav | primary | context) and mobile (Narrow: page host).

```csharp
using Novolis.Avalonia.Layout;

var workspace = new AuthoringWorkspace(navList, primaryEditor, contextInspector)
{
    TopBar = commandBar,       // optional
    StatusBar = statusLine,    // optional
    NavWidth = 280,
    ContextWidth = 320,
    NarrowWidthThreshold = 900,
};

// Adaptive: Bounds.Width < threshold → Narrow (unless ForceMode).
// Lock desktop Wide:
workspace.ForceMode = true;
workspace.LayoutMode = AuthoringLayoutMode.Wide;

// Narrow page cycle (mobile / small window):
workspace.ShowRegion(AuthoringRegion.Nav);
workspace.ShowRegion(AuthoringRegion.Primary);
workspace.ShowRegion(AuthoringRegion.Context);
```

| API | Role |
|-----|------|
| `AuthoringLayoutMode` | `Wide` / `Narrow` |
| `AuthoringRegion` | `Nav` / `Primary` / `Context` |
| `LayoutMode` / `ForceMode` | Active mode; force locks against width adaptation |
| `ShowRegion` | Sets `VisibleRegion` for Narrow page host |
| `Nav` / `Primary` / `Context` | Injectable region content |
| `TopBar` / `StatusBar` | Optional chrome rows |

## Quick start — AnalyzerWorkspace

```csharp
using Novolis.Avalonia.Layout;

var workspace = new AnalyzerWorkspace(packetList, protocolTree, hexDump);
workspace.FilterBar.ApplyRequested += (_, expr) => { /* apply BPF */ };
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Avalonia.Controls` | Sortable data grid, hex dump, detail tree, writer atoms |
| `Novolis.Avalonia.Studio` | Status/flash/busy chrome; `StudioWorkspace` façades Wide `AuthoringWorkspace` |

## More documentation

- [Getting started](https://github.com/Novolis-Platform/novolis-avalonia/blob/main/docs/getting-started.md)
- [Design](https://github.com/Novolis-Platform/novolis-avalonia/blob/main/docs/design.md)
- [Composition grain](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/avalonia-composition-grain.md)

## Support

Pre-release. API may change with Avalonia upgrades.
