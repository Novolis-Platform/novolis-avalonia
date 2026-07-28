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

**Editor features:** AvaloniaEdit source surface, Markdown syntax highlighting (including dialogue, `[!metadata]`, TK/TODO/FIXME in BookAuthoring profile), line numbers, word wrap, current-line highlight, Ctrl+mouse wheel zoom.  
Use `MarkdownSpanAnalyzer` for portable span analysis without AvaloniaEdit.  
**Preview features:** Markdig GFM HTML with Mermaid diagram rendering (via Mermaider), built-in studio/GitHub themes, 10% side margins, Ctrl+scroll zoom.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Markup.Markdown.Rendering` | Standalone HTML/PDF file export (QuestPDF) |
| `Novolis.Avalonia.Studio` | Studio chrome, toolbars, status lines |
