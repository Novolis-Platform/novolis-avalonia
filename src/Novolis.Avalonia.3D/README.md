# Novolis.Avalonia.3D

Hard-surface Avalonia modeling surface (Object Manager + viewport):

- **Object Manager** + property inspector + Raylib viewport with light/camera gizmos
- Light set: Omni, Spot, Infinite, Area
- Mutations via `SceneSessionService.Execute` (UI + LLM parity)
- Agent attach: `AgentSurface.AttachAll(session, SceneSessionContract.Definition)` — HTTP `:18785`, TCP `:18786`

Depends on `Novolis.Modeling.Scene` and `Novolis.Agent.Surface`.

## Install

```bash
dotnet add package Novolis.Avalonia.3D
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), Avalonia.

## Quick start

```csharp
using Novolis.Agent.Surface;
using Novolis.Avalonia._3D.Session;

var session = new SceneSessionService(document);
await using var surface = AgentSurface.AttachAll(session, SceneSessionContract.Definition);
```
