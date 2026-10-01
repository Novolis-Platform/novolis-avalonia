<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-avalonia/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-avalonia/) · [Source](https://github.com/Novolis-Platform/novolis-avalonia)
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
| `ILocationReadingSource` | Produces sparse, provider-neutral geographic readings |
| `IWifiObservationSource` | Reads the currently connected Wi-Fi network when the platform permits it |
| `MobileSourceStatus` | Reports unavailable, denied, disabled, redacted, or available capability states |
| Platform packages | Supply OS-backed implementations of those abstractions |

Sensor sources expose platform evidence only. They do not define places,
presence policy, arrival/departure events, or background scheduling.

## Dogfooding

Books Mobile (`novolis-apps/src/BooksMobile`) consumes this stack on Desktop and Android.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Avalonia.Mobile.Desktop` | Windows Credential Manager + LocalAppData + system browser |
| `Novolis.Avalonia.Mobile.Android` | Keystore-backed prefs + FilesDir + Custom Tabs |

