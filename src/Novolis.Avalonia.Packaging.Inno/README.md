<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-avalonia/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-avalonia/) · [Source](https://github.com/Novolis-Platform/novolis-avalonia)
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Packaging.Inno

Generate Inno Setup `.iss` scripts and MSBuild properties for Novolis Avalonia installers.

Installers are **per-user** (no admin): `PrivilegesRequired=lowest` under `%LocalAppData%\Programs\…`.

Brand defaults:

| Field | Value |
|-------|-------|
| Publisher | `Novolis` |
| Publisher URL | `https://github.com/Novolis-Platform` |
| Copyright | `Copyright (c) Novolis` |
| License | Repo-root `LICENSE` (MIT) when present |
| Setup icon | `ApplicationIcon` or repo-root `icon.ico` |

## Install

```bash
dotnet add package Novolis.Avalonia.Packaging.Inno
```

## Quick start

```csharp
var iss = new InnoScriptGenerator
{
    AppName = "Draft Studio",
    AppVersion = "2026.1.6.123",
    PublishDir = @"artifacts\draft-studio\app",
    AppExeName = "DraftStudio.exe",
    OutputDir = @"artifacts\draft-studio\installer",
    AppId = "Novolis.DraftStudio",
    LicenseFile = @"LICENSE",
    SetupIconFile = @"icon.ico",
}.Generate();
```

The package also ships MSBuild targets. Set the `NovolisInno*` properties (see targets file), then invoke `NovolisGenerateInnoScript`.

