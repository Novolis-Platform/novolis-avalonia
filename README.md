# novolis-avalonia

Reusable **no-XAML** Avalonia controls and layouts for Novolis desktop tools (packet analyzers, studios, inspectors).

## Packages

| Package | Description |
|---------|-------------|
| `Novolis.Avalonia.Layout` | `AnalyzerWorkspace`, `ToolbarRow`, `FilterBar`, `DetailTreeNode` |
| `Novolis.Avalonia.Controls` | `HexDumpView`, `TreeDetailsView`, `PacketTableView`, `HexDumpFormatter` |
| `Novolis.Avalonia.Rendering` | `TwoDSceneControl` (OpenGL TwoD), `Rgba32FrameControl` (CPU / path trace) |
| `Novolis.Avalonia.Raylib` | `RaylibHostControl` (embedded Raylib viewport) |

## Build

```bash
dotnet build
dotnet test
```

## Samples

```bash
dotnet run --project samples/RenderingAvalonia
```

Side-by-side **TwoD** (OpenGL) and **CPU RGBA** frame hosts.

## Dogfood app

[WireFish Viewer](../novolis-dogfooding/apps/WireFishViewer) — live capture UI for `Novolis.Transports.WireFish` (WireShark-inspired layout).

```bash
cd novolis-dogfooding
dotnet run --project apps/WireFishViewer
```

On Windows, install [Npcap](https://npcap.com/) for live capture.

## Templates

For new Avalonia apps, see [novolis-templates](https://github.com/Novolis-Platform/novolis-templates) (`novolis-noxaml-avalonia-sln`).
