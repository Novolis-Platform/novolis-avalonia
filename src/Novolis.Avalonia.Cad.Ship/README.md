<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-avalonia/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-avalonia/) · [Source](https://github.com/Novolis-Platform/novolis-avalonia)
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Cad.Ship

Freighter / ship CAD chrome for hosts that need sealed exterior massing and ship-workspace import. Generic CAD stays in `Novolis.Avalonia.Cad`.

## Install

```bash
dotnet add package Novolis.Avalonia.Cad.Ship
```

## Quick start

```csharp
using Novolis.Avalonia.Cad.Ship;

CadShipChrome.Attach(cadSession); // registers importship + exterior hooks
```

## API

| Type | Role |
|------|------|
| `CadShipChrome.Attach` | Wire exterior hooks + `importship` action |
| `CadShipExterior` | Sealed freighter silhouette for Model view |
| `CadShipImport` | Copy generated `.cadjson` into `ship-workspace` |

Draft Studio and Calypso CAD call `Attach` at startup. Author freighters in **Ship Designer**; keep this package for exterior/import chrome.

