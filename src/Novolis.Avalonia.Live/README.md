# Novolis.Avalonia.Live

Avalonia controls for live audio transport snapshots and program graphs.

## Install

```bash
dotnet add package Novolis.Avalonia.Live
```

## Quick start

```csharp
using Novolis.Avalonia.Live;
using Novolis.Audio.Live.Protocol.Dto;
using Novolis.Audio.Live.Visuals;

var panel = new LiveStudioPanel();
panel.Bind(
    new LiveTransportSnapshotDto(null, null, 0m, 0m, 1, 1, null, null, null),
    null);
```
