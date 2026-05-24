# Novolis.Avalonia.Controls

Reusable Avalonia controls for packet analyzers: data grid, hex dump, and detail tree (code-only, no XAML).

## Install

```bash
dotnet add package Novolis.Avalonia.Controls
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), Avalonia 11. References `Novolis.Avalonia.Layout`.

## Quick start

```csharp
using Novolis.Avalonia.Controls;

var table = new PacketTableView();
table.SetColumns([PacketTableView.TextColumn("#", "Index", 48)]);
var hex = new HexDumpView();
hex.SetBytes(packetBytes);
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Avalonia.Layout` | Analyzer workspace shell and filter bar |

## More documentation

- [Getting started](https://github.com/Novolis-Platform/novolis-avalonia/blob/main/docs/getting-started.md)
- [Design](https://github.com/Novolis-Platform/novolis-avalonia/blob/main/docs/design.md)

## Support

Pre-release. Depends on Avalonia DataGrid package.
