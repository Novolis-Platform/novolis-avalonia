# Novolis.Avalonia.Studio

Studio chrome for Avalonia editor apps: status/flash lines, busy overlay, and three-column layout helpers.

## Install

```bash
dotnet add package Novolis.Avalonia.Studio
```

## Quick start

```csharp
using Novolis.Avalonia.Studio;

var chrome = StudioChrome.Create();
var feedback = chrome.CreateFeedback();
var root = new StudioWorkspace(leftRail, centerColumn, rightRail);
```
