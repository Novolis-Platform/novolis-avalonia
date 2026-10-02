# Novolis.Avalonia.Updates

Avalonia update status, notification, and handoff controls for direct-release
applications. The package supplies the profile-bound view; the product host
owns the actual notification, file reveal, and installer/package handoff.

## Install

```powershell
dotnet add package Novolis.Avalonia.Updates
```

Requires .NET 10, Avalonia, `Novolis.Registry.Updates`, and
`Novolis.Avalonia.GraphicalProfile`.

## Quick start

```csharp
using Novolis.Avalonia.Updates;

var updateView = new UpdateStatusView
{
    Coordinator = coordinator,
    HostActions = hostActions,
    NotificationMode = UpdateNotificationMode.Toast,
};
```

`coordinator` is configured with the neutral registry updater and a
`Novolis.Registry.GitHub` source. `hostActions` opens the release page, shows a
toast or popup, reveals downloaded files, and hands a verified artifact to the
product installer. Set `NotificationMode` to `Inline`, `Toast`, or `Popup`.

The view uses Graphical Profile resources and stable automation IDs such as
`UpdateStatusView`, `UpdateStatusView.CheckButton`, and
`UpdateStatusView.DownloadButton`.

## Related packages

| Package | Role |
| --- | --- |
| `Novolis.Registry.Updates` | Polling, state, verification, and handoff contracts |
| `Novolis.Registry.GitHub` | GitHub Releases update source |
| `Novolis.Avalonia.GraphicalProfile` | Shared light/dark shell resources |

## Support

Direct-release updater component; pre-release API.
