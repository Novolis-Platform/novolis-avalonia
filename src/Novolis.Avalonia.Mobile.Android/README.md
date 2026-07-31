# Novolis.Avalonia.Mobile.Android

Android implementations of `Novolis.Avalonia.Mobile`: Android Keystore AES-GCM + private SharedPreferences, `{FilesDir}/{product}/workspace`, Custom Tabs.

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
| `AddNovolisMobileAndroid(productName)` | Wires Keystore AES-GCM prefs, `{FilesDir}/{product}/workspace`, Custom Tabs |

## Dogfooding

```powershell
dotnet build novolis-apps/src/BooksMobile/BooksMobile.Android
```

Host-side APK install / device stats: `Novolis.IO.Mobile.Android` + dogfood `AdbLab`.

## Build / pack

Not included in `Novolis.Avalonia.slnx` (Linux CI lacks the Android workload). Pack on a workload-enabled runner and publish to **GitHub Packages** (no local feeds):

```bash
dotnet workload install android
dotnet pack src/Novolis.Avalonia.Mobile.Android/Novolis.Avalonia.Mobile.Android.csproj -c Release
```

## Related

| Package | Role |
|---------|------|
| `Novolis.Avalonia.Mobile` | Abstractions contracts |
| `Novolis.Avalonia.Mobile.Desktop` | Desktop counterpart |
