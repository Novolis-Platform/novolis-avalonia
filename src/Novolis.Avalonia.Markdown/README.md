<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Markdown

No-XAML Avalonia controls for Markdown editing and live HTML preview.

## Install

```bash
dotnet add package Novolis.Avalonia.Markdown
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), Avalonia 12.

Add AvaloniaEdit to your app project and include its styles (required for the editor to render):

```xml
<!-- .csproj -->
<PackageReference Include="Avalonia.AvaloniaEdit" Version="12.0.0" />
```

```xml
<!-- App.axaml -->
<Application.Styles>
  <FluentTheme />
  <StyleInclude Source="avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml" />
</Application.Styles>
```

## Quick start

```csharp
using Avalonia.Controls;
using Novolis.Avalonia.Markdown;

// Split editor + debounced preview (independent Ctrl+scroll zoom per pane)
var studio = new MarkdownEditorPreview
{
    Text = "# Hello\n\nEdit **here**, preview updates live.",
};

// Or compose individual controls
var editor = new MarkdownSourceEditor { WordWrap = true };
var preview = new MarkdownPreviewPane();
```

## Controls

| Type | Purpose |
|------|---------|
| `MarkdownEditorPreview` | Split editor + live preview |
| `MarkdownSourceEditor` | AvaloniaEdit source surface with syntax highlighting |
| `MarkdownPreviewPane` | Novolis Markdown HTML preview with Mermaid |
| `MarkdownSpanAnalyzer` | Portable span analysis without AvaloniaEdit |

**Editor features:** syntax highlighting (dialogue, `[!metadata]`, TK/TODO/FIXME in BookAuthoring profile), line numbers, word wrap, current-line highlight, Ctrl+mouse wheel zoom.  
**Preview features:** Mermaid diagrams (via `Novolis.Avalonia.Mermaid`), built-in studio/GitHub themes, 10% side margins, Ctrl+scroll zoom.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Markup.Markdown` | Markdown parse + HTML convert used by preview |
| `Novolis.Avalonia.Studio` | Studio chrome, toolbars, status lines |

