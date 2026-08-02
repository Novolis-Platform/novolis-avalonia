# novolis-avalonia

Reusable **no-XAML** Avalonia controls and layouts for Novolis desktop tools (packet analyzers, studios, inspectors).

## Packages

| Package | Description |
|---------|-------------|
| [`Novolis.Avalonia.Layout`](src/Novolis.Avalonia.Layout/README.md) | `AnalyzerWorkspace`, `ToolbarRow`, `FilterBar`, `DetailTreeNode` |
| [`Novolis.Avalonia.Controls`](src/Novolis.Avalonia.Controls/README.md) | `HexDumpView`, `TreeDetailsView`, `PacketTableView`, `SketchControl` |
| [`Novolis.Avalonia.Rendering`](src/Novolis.Avalonia.Rendering/README.md) | `TwoDSceneControl` (OpenGL TwoD), `Rgba32FrameControl` (CPU / path trace) |
| [`Novolis.Avalonia.Gaming`](src/Novolis.Avalonia.Gaming/README.md) | Game shell: HUD + modal menus over Silk/TwoD viewport (`HardPause` default) |
| [`Novolis.Avalonia.Raylib`](src/Novolis.Avalonia.Raylib/README.md) | `RaylibHostControl` (embedded Raylib viewport) |
| [`Novolis.Avalonia.Live`](src/Novolis.Avalonia.Live/README.md) | Live editor, DSL completion/compiler, visualizers, transport panels |
| [`Novolis.Avalonia.Markdown`](src/Novolis.Avalonia.Markdown/README.md) | Markdown editor, live HTML preview, split-pane studio controls |
| [`Novolis.Avalonia.Mermaid`](src/Novolis.Avalonia.Mermaid/README.md) | Mermaid diagram control (SVG via Mermaider) |
| [`Novolis.Avalonia.StarMap`](src/Novolis.Avalonia.StarMap/README.md) | Pan/zoom star map for catalog points and route edges |
| [`Novolis.Avalonia.Studio`](src/Novolis.Avalonia.Studio/README.md) | Studio chrome: status, flash, busy overlay |
| [`Novolis.Avalonia.Briefing`](src/Novolis.Avalonia.Briefing/README.md) | Briefing primitives: feed, scorecard, dual metric strip, metric table |
| [`Novolis.Avalonia.Mobile`](src/Novolis.Avalonia.Mobile/README.md) | Secure token store, app-data paths, browser launcher, device-flow helpers |
| [`Novolis.Avalonia.Mobile.Desktop`](src/Novolis.Avalonia.Mobile.Desktop/README.md) | Windows Credential Manager + LocalAppData + system browser |
| [`Novolis.Avalonia.Mobile.Android`](src/Novolis.Avalonia.Mobile.Android/README.md) | Keystore AES-GCM tokens + FilesDir + Custom Tabs |
| [`Novolis.Avalonia.Timeline`](src/Novolis.Avalonia.Timeline/README.md) | Git-graph timeline panels |
| [`Novolis.Avalonia.Voice`](src/Novolis.Avalonia.Voice/README.md) | Voice preset studio UI |
| [`Novolis.Avalonia.3D`](src/Novolis.Avalonia.3D/README.md) | Scene editor / OpenGL 3D renderer surface |
| [`Novolis.Avalonia.Cad`](src/Novolis.Avalonia.Cad/README.md) | Shared CAD surface: Draft Studio, CAD Studio 3D, preview hosts |
| [`Novolis.Avalonia.Agent`](src/Novolis.Avalonia.Agent/README.md) | LocalIpc UI agent host for MCP / tooling |
| [`Novolis.Avalonia.Agent.Protocol`](src/Novolis.Avalonia.Agent.Protocol/README.md) | MessagePack DTOs and `UiAgentClient` RPC client |
| [`Novolis.Avalonia.Packaging.Inno`](src/Novolis.Avalonia.Packaging.Inno/README.md) | Inno Setup `.iss` generation for per-user installers |

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
