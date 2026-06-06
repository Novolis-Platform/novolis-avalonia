# Novolis.Avalonia.Packaging.Inno

Generate Inno Setup `.iss` scripts and MSBuild properties for Novolis Avalonia installers.

## Install

```bash
dotnet add package Novolis.Avalonia.Packaging.Inno
```

## Quick start

```csharp
var iss = new InnoScriptGenerator
{
    AppName = "Novolis Audio Live",
    AppVersion = "2026.1.6.123",
    PublishDir = @"artifacts\live-studio\app",
    AppExeName = "Novolis.Audio.Live.Studio.exe",
    OutputDir = @"artifacts\live-studio\installer",
}.Generate();
```

The package also ships MSBuild targets that can emit the installer script when you set:

```xml
<NovolisInnoAppName>Novolis Audio Live</NovolisInnoAppName>
<NovolisInnoPublishDir>artifacts\live-studio\app</NovolisInnoPublishDir>
<NovolisInnoOutputDir>artifacts\live-studio\installer</NovolisInnoOutputDir>
```

Then invoke `NovolisGenerateInnoScript` or let the release workflow call it directly.
