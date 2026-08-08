<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Cad.Ship

Freighter / ship CAD chrome for hosts that need sealed exterior massing and ship-workspace import. Generic CAD stays in `Novolis.Avalonia.Cad`.

## Install

```bash
dotnet add package Novolis.Avalonia.Cad.Ship
```

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

Draft Studio and Calypso CAD call `Attach` at startup.
