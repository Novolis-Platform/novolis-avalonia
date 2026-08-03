<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Live

Avalonia controls for Novolis Audio Live: transport panels, program graphs, DSL code editor, script compiler, and visualizers.

## Install

```bash
dotnet add package Novolis.Avalonia.Live
```

Host apps that use `LiveCodeEditorControl` should include AvaloniaEdit Fluent styles:

```xml
<StyleInclude Source="avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml" />
```

## Quick start

```csharp
using Novolis.Avalonia.Live;
using Novolis.Audio.Live.Protocol.Dto;
using Novolis.Audio.Live.Visuals;

var panel = new LiveStudioPanel();
panel.Bind(
    new LiveTransportSnapshotDto(null, null, 0m, 0m, 1, 1, null, null, null),
    null);

var editor = new LiveCodeEditorControl();
var compiler = new LiveScriptCompiler();
```

