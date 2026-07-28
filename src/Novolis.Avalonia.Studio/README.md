# Novolis.Avalonia.Studio

Studio chrome for Avalonia editor apps: status/flash lines, busy overlay, three-column layout, focus mode, and dirty/clean status brushes.

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

// Focus mode hides chrome controls
StudioFocusMode.Apply(focused: true, menu, topBar, statusBar);

// Dirty status bar
statusBar.Background = StudioStatusBrushes.ForDirtyState(isDirty);
```
