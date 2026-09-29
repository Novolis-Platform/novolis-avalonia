<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Mobile.Android

Android implementations of `Novolis.Avalonia.Mobile`: Android Keystore AES-GCM,
private SharedPreferences, `{FilesDir}/{product}/workspace`, Custom Tabs, sparse
location readings, and connected-Wi-Fi observation.

## Install

```bash
dotnet add package Novolis.Avalonia.Mobile.Android
```

## Quick start

```csharp
using Novolis.Avalonia.Mobile.Android;

services.AddNovolisMobileCore();
services.AddNovolisMobileAndroid("BooksMobile"); // product folder under FilesDir
```

Requires `net10.0-android` and a running Android application context.

## API

| Surface | Role |
|---------|------|
| `AddNovolisMobileAndroid(productName)` | Wires storage, browser, location, and Wi-Fi platform services |

`AndroidLocationReadingSource` uses Android location providers and emits
platform readings only. `AndroidWifiObservationSource` reads the connected SSID
without scanning nearby networks. Permission denial, disabled services, and
SSID redaction are returned as explicit capability states.

## Dogfooding

```powershell
dotnet build novolis-apps/src/BooksMobile/BooksMobile.Android
```

Host-side APK install / device diagnostics:
`Novolis.IO.Mobile.Android`, the `novolis-android` PackAsTool, and the
`d:\novolis\novolis-utilities\src\Adb` visual utility.

## Build / pack

The solution includes this project; CI installs the Android workload before
building and packing. Publish to **GitHub Packages** (no local feeds):

```bash
dotnet workload install android
dotnet pack src/Novolis.Avalonia.Mobile.Android/Novolis.Avalonia.Mobile.Android.csproj -c Release
```

## Related

| Package | Role |
|---------|------|
| `Novolis.Avalonia.Mobile` | Abstractions contracts |
| `Novolis.Avalonia.Mobile.Desktop` | Desktop counterpart |

