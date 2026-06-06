# Release

See [release policy](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/release-policy.md).

## What ships

- NuGet packages continue to publish through the shared Novolis release pipeline.
- The Windows live studio ships as an Inno Setup installer attached to the GitHub Release.
- The installer is per-user and installs to `%LOCALAPPDATA%\Programs\Novolis\Novolis Audio Live`.

## Release flow

1. The release tag is validated against `build/version.json`.
2. The Avalonia studio and the headless live host are published as `win-x64` payloads.
3. The installer script is generated from the published app payload.
4. Inno Setup compiles the installer.
5. The resulting `.exe` is uploaded to the same GitHub Release as the NuGet packages.

## User guidance

- Download the installer from the release assets page.
- Run it without administrator privileges.
- Update by installing a newer release over the existing user-space install.
