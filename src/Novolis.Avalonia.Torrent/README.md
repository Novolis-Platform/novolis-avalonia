<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Torrent

Avalonia torrent session chrome bound to [`Novolis.Transports.Torrent`](https://github.com/Novolis-Platform/novolis-transports) — not a product host.

## Install

```bash
dotnet add package Novolis.Avalonia.Torrent
```

## Quick start

```csharp
using Novolis.Avalonia.Torrent;

var panel = new TorrentSessionPanel(/* session */);
```

Dogfood: TorrentLab under novolis-dogfooding.
