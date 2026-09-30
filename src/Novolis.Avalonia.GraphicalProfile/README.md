# Novolis.Avalonia.GraphicalProfile

Required Avalonia chrome for Novolis product hosts. Role values are generated
from the governance bundle in
`novolis-governance/build/graphical-profile/profile.json`.

## Install

```bash
dotnet add package Novolis.Avalonia.GraphicalProfile
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), Avalonia.

## Quick start

```csharp
using Novolis.Avalonia.GraphicalProfile;

public sealed class App : Application
{
    public override void Initialize()
    {
        GraphicalProfile.Install(this);
    }
}

GraphicalProfileBinding.Bind(
    title,
    TextBlock.ForegroundProperty,
    GraphicalProfile.TextResourceKey);
```

`RequestedThemeVariant` stays at the operating-system default. Use `Ngp.*`
resources, `GraphicalProfileBinding.Bind`, or the class styles for eyebrow,
page title, body, nav, accent button, action button, and card.

## Support

Pre-release.
