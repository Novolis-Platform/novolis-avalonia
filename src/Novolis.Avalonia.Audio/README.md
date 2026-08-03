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

## Quick start

```csharp
var project = new MusicProject("Demo");
AudioEditOps.AddTrack(project, "Lead");
using var workspace = new AudioEditWorkspace(project);
window.Content = workspace;
```

## Install

```bash
dotnet add package Novolis.Avalonia.Audio
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).


