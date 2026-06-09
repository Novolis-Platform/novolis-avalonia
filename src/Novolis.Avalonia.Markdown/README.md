# Novolis.Avalonia.Markdown

No-XAML Avalonia controls for Markdown editing and live HTML preview.

## Install

```bash
dotnet add package Novolis.Avalonia.Markdown
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), Avalonia 12.

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

**Editor features:** line-number gutter, word wrap, active-line marker, Ctrl+mouse wheel zoom.  
**Preview features:** Markdig HTML with built-in studio/GitHub themes, Ctrl+scroll zoom.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Markup.Markdown.Rendering` | Standalone HTML/PDF file export (QuestPDF) |
| `Novolis.Avalonia.Studio` | Studio chrome, toolbars, status lines |
