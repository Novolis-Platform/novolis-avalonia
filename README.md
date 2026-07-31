# novolis-avalonia

Reusable **no-XAML** Avalonia controls and layouts for Novolis desktop tools (packet analyzers, studios, inspectors).

## Packages

| Package | Description |
|---------|-------------|
| `Novolis.Avalonia.Layout` | `AnalyzerWorkspace`, `ToolbarRow`, `FilterBar`, `DetailTreeNode` |
| `Novolis.Avalonia.Controls` | `HexDumpView`, `TreeDetailsView`, `PacketTableView`, `HexDumpFormatter` |
| `Novolis.Avalonia.Rendering` | `TwoDSceneControl` (OpenGL TwoD), `Rgba32FrameControl` (CPU / path trace) |
| `Novolis.Avalonia.Raylib` | `RaylibHostControl` (embedded Raylib viewport) |
| `Novolis.Avalonia.Live` | Live editor, DSL completion/compiler, visualizers, transport panels |
| `Novolis.Avalonia.Markdown` | Markdown editor, live HTML preview, split-pane studio controls |
| `Novolis.Avalonia.Mermaid` | Mermaid diagram control (SVG via Mermaider) |
| `Novolis.Avalonia.StarMap` | Pan/zoom star map for catalog points and route edges |
| `Novolis.Avalonia.Studio` | Studio chrome: status, flash, busy overlay |
| `Novolis.Avalonia.Briefing` | Briefing primitives: feed, scorecard, dual metric strip, metric table |
| `Novolis.Avalonia.Mobile` | Secure token store, app-data paths, browser launcher, device-flow helpers |
| `Novolis.Avalonia.Mobile.Desktop` | Windows Credential Manager + LocalAppData + system browser |
| `Novolis.Avalonia.Mobile.Android` | Keystore AES-GCM tokens + FilesDir + Custom Tabs |
| `Novolis.Avalonia.Timeline` | Git-graph timeline panels |
| `Novolis.Avalonia.Voice` | Voice preset studio UI |

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

Launches the live studio dashboard, starts the headless audio host, connects over local IPC, compiles a typed showcase set, and renders timing, diagnostics, and the live program graph together.

## Releases

Windows users can download the shipped live studio installer from the GitHub Release assets. The installer runs in user space, installs under `%LOCALAPPDATA%\Programs\Novolis\Novolis Audio Live`, and does not require administrator privileges.

The release pipeline still publishes NuGet packages for library consumers. The installer is an additional release asset for the end-user app.

## Dogfood app

[WireFish Viewer](../novolis-dogfooding/apps/WireFishViewer) — live capture UI for `Novolis.Transports.WireFish` (WireShark-inspired layout).

```bash
cd novolis-dogfooding
dotnet run --project apps/WireFishViewer
```

On Windows, install [Npcap](https://npcap.com/) for live capture.

## Templates

For new Avalonia apps, see [novolis-templates](https://github.com/Novolis-Platform/novolis-templates) (`novolis-noxaml-avalonia-sln`).
