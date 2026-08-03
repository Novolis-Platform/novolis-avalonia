<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Mobile

Platform abstractions for Avalonia mobile/desktop apps: secure token store, app-data paths, browser launcher, and GitHub device-flow presentation helpers.

## Install

```bash
dotnet add package Novolis.Avalonia.Mobile
```

## Quick start

```csharp
using Microsoft.Extensions.DependencyInjection;
using Novolis.Avalonia.Mobile;

services.AddNovolisMobileCore();
// Then AddNovolisMobileDesktop() or AddNovolisMobileAndroid(...) from the platform package.
```

## API

| Surface | Role |
|---------|------|
| `AddNovolisMobileCore()` | Registers shared mobile abstractions (token store, paths, browser, device-flow UI hooks) |
| Platform packages | Supply OS-backed implementations of those abstractions |

## Dogfooding

Books Mobile (`novolis-apps/src/BooksMobile`) consumes this stack on Desktop and Android.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Avalonia.Mobile.Desktop` | Windows Credential Manager + LocalAppData + system browser |
| `Novolis.Avalonia.Mobile.Android` | Keystore-backed prefs + FilesDir + Custom Tabs |

