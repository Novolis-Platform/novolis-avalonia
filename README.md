# novolis-avalonia

Reusable **no-XAML** Avalonia controls and layouts for Novolis desktop tools (packet analyzers, studios, inspectors).

## Packages

| Package | Description |
|---------|-------------|
| `Novolis.Avalonia.Layout` | `AnalyzerWorkspace`, `ToolbarRow`, `FilterBar`, `DetailTreeNode` |
| `Novolis.Avalonia.Controls` | `HexDumpView`, `TreeDetailsView`, `PacketTableView`, `HexDumpFormatter` |
| `Novolis.Avalonia.Rendering` | `TwoDSceneControl` (OpenGL TwoD), `Rgba32FrameControl` (CPU / path trace) |
| `Novolis.Avalonia.Raylib` | `RaylibHostControl` (embedded Raylib viewport) |
| `Novolis.Avalonia.Live` | Live audio panels for transport snapshots and program graphs |

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

```bash
dotnet run --project samples/LiveAvalonia
```

Launches the live studio sample, starts the headless audio host, connects over local IPC, compiles a typed program, and renders the current snapshot/graph view.

## Dogfood app

[WireFish Viewer](../novolis-dogfooding/apps/WireFishViewer) — live capture UI for `Novolis.Transports.WireFish` (WireShark-inspired layout).

```bash
cd novolis-dogfooding
dotnet run --project apps/WireFishViewer
```

On Windows, install [Npcap](https://npcap.com/) for live capture.

## Templates

For new Avalonia apps, see [novolis-templates](https://github.com/Novolis-Platform/novolis-templates) (`novolis-noxaml-avalonia-sln`).
