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
