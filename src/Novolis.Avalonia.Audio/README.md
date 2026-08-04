<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Audio

Lightweight Magix Music Maker / Audacity–style Avalonia editor chrome for `Novolis.Audio.Edit`.

## Controls

| Control | Role |
|---------|------|
| `AudioEditWorkspace` | Full shell (tasks / library / timeline / clip envelope) |
| `AudioLibraryControl` | Sound pool with mini waveforms |
| `ArrangementTimelineControl` | Multi-track timeline + playhead |
| `WaveformControl` | Audacity-style peak view |
| `ClipInspectorControl` | Gain / fade in / fade out |
| `AudioTransportBar` | Rewind / play-pause |
| `NaudioPreviewPlayer` | Mix preview via NAudio |
| `MidiPianoWorkspace` | MIDI piano + full score + piano-roll + PDF |
| `PianoRollControl` | Beat-grid piano-roll editor |
| `ScoreStaffControl` | Grand-staff score preview |
| `PianoKeyboardControl` | On-screen piano keys |
| `InstrumentBrowserControl` | Browse patch categories |
| `MediaCatalogWorkspace` | Browse free collections, map Artlist-style inspiration URLs, download + transformers |

## Quick start

```csharp
var project = new MusicProject("Demo");
AudioEditOps.AddTrack(project, "Lead");
using var workspace = new AudioEditWorkspace(project);
window.Content = workspace;
```

Catalog / explore:

```csharp
var catalog = new MediaCatalogWorkspace();
catalog.ScoreProduced += score => piano.ApplyScore(score);
// Paste https://artlist.io/… → Map inspiration → free cinematic stand-in collection
```

## Install

```bash
dotnet add package Novolis.Avalonia.Audio
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).


