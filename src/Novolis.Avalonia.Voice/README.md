# Novolis.Avalonia.Voice

Avalonia controls for designing Novolis voice presets: archetype + ATC inspectors, live preview, and C# export.

## Install

```bash
dotnet add package Novolis.Avalonia.Voice
dotnet add package Novolis.Audio.Voice.Design
```

## Quick start

```csharp
using Novolis.Avalonia.Studio;
using Novolis.Avalonia.Voice;

var chrome = StudioChrome.Create();
var feedback = chrome.CreateFeedback();
Content = new VoiceStudioPanel(feedback);
```
