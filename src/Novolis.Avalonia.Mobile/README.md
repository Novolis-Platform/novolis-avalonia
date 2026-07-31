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
// Plus AddNovolisMobileDesktop() or AddNovolisMobileAndroid() from the platform package.
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Avalonia.Mobile.Desktop` | Windows Credential Manager + LocalAppData + system browser |
| `Novolis.Avalonia.Mobile.Android` | Keystore-backed prefs + FilesDir + Custom Tabs |
