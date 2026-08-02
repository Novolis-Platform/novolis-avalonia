# Novolis.Avalonia.Voice

Avalonia controls for designing Novolis voice presets: preset list, archetype/effect/platform inspectors, debounced TTS preview, and C# code export.

## Install

```bash
dotnet add package Novolis.Avalonia.Voice
dotnet add package Novolis.Avalonia.Studio
dotnet add package Novolis.Audio.Voice.Design
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), Avalonia.

## Quick start

```csharp
using Novolis.Avalonia.Studio;
using Novolis.Avalonia.Voice;

var feedback = StudioChrome.Create().CreateFeedback();
var preview = new VoicePreviewController();
var studio = new VoiceStudioPanel(feedback, preview);
Content = studio;
```

Set `VoicePreviewController.PlatformPreviewFactory` when using `VoiceSynthesizerBackend.Platform` on Windows.

## API

| API | Purpose |
|-----|---------|
| `VoiceStudioPanel` | Composite studio: presets, tabs, preview bar, code export |
| `VoiceStudioPanel(StudioFeedback)` | Minimal ctor (default preview + export) |
| `VoiceStudioPanel(StudioFeedback, VoicePreviewController, IVoicePresetCodeExport?)` | Full ctor |
| `VoiceStudioPanel.PreviewController` | Access debounced preview controller |
| `VoiceStudioPanel.Presets` | `VoicePresetListBox` for host seeding |
| `VoicePresetListBox.LoadCatalogSeeds()` | Seed from `VoiceArchetypeCatalog` |
| `VoicePresetListBox.AddDraft` / `CloneSelected` / `AddBlank` | Preset management |
| `VoicePresetListBox.SelectionChangedDraft` | Fires with `VoicePresetDraft` |
| `VoicePreviewController.SchedulePreview(draft)` | Debounced auto-preview (~400 ms) |
| `VoicePreviewController.PreviewNowAsync(draft)` | Immediate preview |
| `VoiceArchetypeInspector.Bind(draft)` | Edit backend, profile, model, rates |
| `VoiceEffectChainInspector.Bind(draft)` | Edit post-TTS effect chain |
| `VoicePlatformInspector.Bind(draft)` | Edit `PlatformSpeechOptions` |
| `VoiceCodeExportPanel` / `IVoicePresetCodeExport` | Generated C# with template picker + copy |

## Related / dogfood

| Package / app | Notes |
|---------------|-------|
| [`Novolis.Avalonia.Studio`](../Novolis.Avalonia.Studio/README.md) | `StudioChrome`, `StudioFeedback` |
| [`Novolis.Audio.Voice.Design`](../../../novolis-audio/src/Novolis.Audio.Voice.Design/README.md) | `VoicePresetDraft`, validation, code emitter |
| [NovolisVoiceStudio](../../../novolis-dogfooding/apps/audio/NovolisVoiceStudio) | Full voice preset studio |
