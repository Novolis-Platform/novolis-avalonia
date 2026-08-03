<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Layout

WireShark-style analyzer shell layout: toolbar, filter bar, and split panes (no XAML).

## Install

```bash
dotnet add package Novolis.Avalonia.Layout
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), Avalonia 11.

## Quick start

```csharp
using Novolis.Avalonia.Layout;

var workspace = new AnalyzerWorkspace(packetList, protocolTree, hexDump);
workspace.FilterBar.ApplyRequested += (_, expr) => { /* apply BPF */ };
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Avalonia.Controls` | Packet table, hex dump, detail tree controls |

## More documentation

- [Getting started](https://github.com/Novolis-Platform/novolis-avalonia/blob/main/docs/getting-started.md)
- [Design](https://github.com/Novolis-Platform/novolis-avalonia/blob/main/docs/design.md)

## Support

Pre-release. API may change with Avalonia upgrades.

