<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-avalonia/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-avalonia/) · [Source](https://github.com/Novolis-Platform/novolis-avalonia)
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Speech

Application speech front plus interconnected Avalonia controls for selecting,
configuring, and using speech providers.

**Default:** Edge Read Aloud (online MP3, no Azure credentials).  
**Optional:** Azure Speech via Microsoft Entra login (tenant / subscription /
resource autolookup) or manual endpoint + key.  
**Fallback:** device voice (playback only).

## Install

```xml
<PackageReference Include="Novolis.Avalonia.Speech" Version="2026.1.*" />
```

```csharp
services.AddNovolisSpeech();
// Optional host-specific Entra discovery:
services.AddSingleton<IAzureSpeechResourcePicker, MyAzureSpeechResourcePicker>();
```

## Interconnected controls

Bind one shared `SpeechWorkspace` (registered by `AddNovolisSpeech`) to any of:

| Control | Role |
| --- | --- |
| `SpeechSettingsPanel` | Composed chrome (providers + status + voice + Azure setup) |
| `SpeechProviderPicker` | Edge / Azure / Device |
| `SpeechStatusCard` | Provider + capability summary |
| `SpeechVoicePicker` | Voice list for the active provider |
| `AzureSpeechSetupPanel` | Entra login cascade or manual endpoint/key |

```csharp
var panel = new SpeechSettingsPanel { Workspace = workspace };
await workspace.InitializeAsync(); // prefers Edge when Azure is not configured
```

## Notes

- Azure credentials are stored through the host `ISecureTokenStore`.
- The package never provides a Novolis relay or subscription.
- Device voice cannot create MP3 files; Edge and configured Azure can.
