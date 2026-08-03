<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Mobile.Desktop

Windows desktop implementations of [`Novolis.Avalonia.Mobile`](../Novolis.Avalonia.Mobile/README.md): Credential Manager token store, `%LocalAppData%\Novolis\{product}\` app-data paths, and system-browser launch for OAuth device flows.

## Install

```bash
dotnet add package Novolis.Avalonia.Mobile.Desktop
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), Avalonia, Windows. References `Novolis.Avalonia.Mobile`.

## Quick start

```csharp
using Microsoft.Extensions.DependencyInjection;
using Novolis.Avalonia.Mobile.Desktop;

services.AddNovolisMobileDesktop("BooksMobile");
// Resolves: ISecureTokenStore, IAppDataPaths, IBrowserLauncher, IDeviceFlowPresenter
```

## API

| API | Purpose |
|-----|---------|
| `DesktopMobileServiceCollectionExtensions.AddNovolisMobileDesktop(services, productName)` | Registers desktop platform services + `AddNovolisMobileCore()` |
| `WindowsCredentialTokenStore` | `ISecureTokenStore` backed by Windows Credential Manager (`Novolis/` prefix) |
| `WindowsCredentialTokenStore.GetAsync` / `SetAsync` / `RemoveAsync` | Read, write, or delete secrets by key |
| `DesktopAppDataPaths` | `IAppDataPaths` under `%LocalAppData%\Novolis\{product}\` |
| `DesktopAppDataPaths.RootDirectory` | App-private root directory |
| `DesktopAppDataPaths.WorkspaceDirectory` | `{Root}\workspace` (created on construct) |
| `ProcessBrowserLauncher` | `IBrowserLauncher` via `Process.Start` + `UseShellExecute` |
| `ProcessBrowserLauncher.OpenAsync(uri)` | Opens URL in the default OS browser |

## Related / dogfood

| Package / app | Notes |
|---------------|-------|
| [`Novolis.Avalonia.Mobile`](../Novolis.Avalonia.Mobile/README.md) | Platform abstractions (`ISecureTokenStore`, `IAppDataPaths`, …) |
| [`Novolis.Avalonia.Mobile.Android`](../Novolis.Avalonia.Mobile.Android/README.md) | Android Keystore + Custom Tabs counterpart |
| [BooksMobile](../../../novolis-apps/src/BooksMobile) | OAuth device-flow desktop host |

