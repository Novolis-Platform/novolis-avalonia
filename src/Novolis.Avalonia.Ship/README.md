<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Ship

Ship Designer UI chrome for Avalonia Cad hosts: validate ship, airtight overlay, hatch place helpers. Composes `Novolis.Avalonia.Cad`, `Novolis.Avalonia.Cad.Ship`, and `Novolis.Ship.*`.

## Install

```bash
dotnet add package Novolis.Avalonia.Ship
```

## Quick start

```csharp
using Novolis.Avalonia.Ship;

ShipChrome.Attach(cadSession); // Cad.Ship exterior/import + validateship / refreshairtight / placehatch
var strip = ShipChrome.CreateToolStrip(cadSession);
```

Host product: **Ship Designer** (`novolis-apps`). Draft Studio exterior/import alone can keep using `CadShipChrome.Attach`.
