# Novolis.Avalonia.Mobile.Android

Android implementations of `Novolis.Avalonia.Mobile`: Android Keystore AES-GCM + private SharedPreferences, `{FilesDir}/{product}/workspace`, Custom Tabs.

## Install

```bash
dotnet add package Novolis.Avalonia.Mobile.Android
```

## Quick start

```csharp
services.AddNovolisMobileAndroid("BooksMobile");
```

Requires `net10.0-android` and a running Android application context.

## Build / pack

Not included in `Novolis.Avalonia.slnx` (Linux CI lacks the Android workload). Pack locally or on a workload-enabled runner:

```bash
dotnet workload install android
dotnet pack src/Novolis.Avalonia.Mobile.Android/Novolis.Avalonia.Mobile.Android.csproj -c Release
```
