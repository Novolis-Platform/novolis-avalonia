<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Mermaid

Avalonia control that renders Mermaid diagrams to SVG via [`Novolis.Markup.Mermaid.Rendering`](https://www.nuget.org/packages/Novolis.Markup.Mermaid.Rendering) (Mermaider under the hood) without a browser.

## Install

```bash
dotnet add package Novolis.Avalonia.Mermaid
```

## Quick start

```csharp
using Novolis.Avalonia.Mermaid;
using Novolis.Markup.Mermaid;

var control = new MermaidControl
{
    DiagramTheme = MermaidTheme.StudioDark,
    Diagram = new SequenceDiagram()
        .AddParticipant("A", "Alice")
        .AddParticipant("B", "Bob")
        .Message("A", "B", "Hello"),
};

// Or raw source:
control.Source = """
    flowchart TD
      A[Start] --> B[Done]
    """;
```

## API

| Type | Role |
|------|------|
| `MermaidControl` | Code-only Avalonia control (`Source` / `Diagram` / `DiagramTheme`) |
| `MermaidSvg` | Static SVG / HTML helpers for custom hosts |
| `MermaidTheme` | `StudioDark` or `GitHubLight` |

Pair with `Novolis.Markup.Mermaid` for fluent builders, or bind any Mermaid source string. Headless SVG/PNG export lives in `Novolis.Markup.Mermaid.Rendering`.

## Related

- `Novolis.Avalonia.Markdown` — Markdown preview that also renders fenced `mermaid` blocks
- `Novolis.Markup.Mermaid` — diagram syntax builders
- `Novolis.Markup.Mermaid.Rendering` — headless SVG/PNG export

