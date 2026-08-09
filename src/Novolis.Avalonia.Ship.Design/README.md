<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Ship.Design

Object-first Ship Designer UI for Avalonia: `ShipDesignSession`, PLAN / MODEL / PRESENT workspaces, create-ship panel, object tools, deck navigation, and contextual properties.

Composes `Novolis.Ship.Design`, `Novolis.Avalonia.Cad`, and `Novolis.Avalonia.Ship` (validate/import forwarder).

## Install

```bash
dotnet add package Novolis.Avalonia.Ship.Design
```

## Quick start

```csharp
using Novolis.Avalonia.Ship.Design;

var session = new ShipDesignSession(dataRoot);
ShipDesignChrome.Attach(cadSession, session);
var shell = ShipDesignChrome.CreateShell(cadSession, session, cadEditorSurface);
```
