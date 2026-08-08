<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Studio

Studio chrome for Avalonia editor apps: status/flash lines, busy overlay, three-column layout (façade over Layout `AuthoringWorkspace` when used), focus mode, dirty/clean status brushes, and a domain-agnostic command bar.

Prefer composing `Novolis.Avalonia.Layout.AuthoringWorkspace` for new hosts; `StudioWorkspace` remains for existing callers.

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

var commandBar = new StudioCommandBar();
commandBar.Submitted += (_, e) => { /* handle e.Text */ };
commandBar.Cancelled += (_, _) => { /* cancel tool / clear */ };

StudioFocusMode.Apply(focused: true, menu, topBar, statusBar);
statusBar.Background = StudioStatusBrushes.ForDirtyState(isDirty);
```

## API

| Type | Role |
|------|------|
| `StudioChrome` | Factory for chrome + feedback helpers |
| `StudioWorkspace` | Three-column editor shell |
| `StudioCommandBar` | Domain-agnostic command entry (`Submitted` / `Cancelled`) |
| `StudioFocusMode` | Hide/show chrome for focus mode |
| `StudioStatusBrushes` | Dirty/clean status brushes |

## Dogfooding

SketchLab, CadStudio3D, DraftStudio, Books Writer Studio, SceneLab.

## Related

| Package | Role |
|---------|------|
| `Novolis.Avalonia.Controls` | Shared controls used inside studio shells |
| `Novolis.Avalonia.Markdown` | Markdown preview often hosted in studio center panes |

