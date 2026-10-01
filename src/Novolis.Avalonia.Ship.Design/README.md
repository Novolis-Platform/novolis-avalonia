<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-avalonia/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-avalonia/) · [Source](https://github.com/Novolis-Platform/novolis-avalonia)
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Ship.Design

Object-first Spacecraft Design UI for Avalonia: `ShipDesignSession`, PLAN / MODEL / ANALYZE workspaces, create-ship panel, semantic hierarchy, continuous GREEN/YELLOW/RED analysis strip, object tools, deck navigation, and contextual properties.

Composes `Novolis.Ship.Design`, `Novolis.Ship.Analysis`, `Novolis.Avalonia.Cad`, and `Novolis.Avalonia.Ship`.

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
