# Novolis.Avalonia.Agent

Embeds a LocalIpc `ui.*` agent host in an Avalonia window so MCP / tooling can dump the visual tree, screenshot, click, type, and select list/tab items.

## Install

```bash
dotnet add package Novolis.Avalonia.Agent
```

## Quick start

```csharp
using Novolis.Avalonia.Agent;

// After MainWindow is created:
AgentHost.TryAttachFromEnvironment(desktop.MainWindow);

// Or always:
AgentHost.Attach(desktop.MainWindow);

// Tag controls for stable ids:
AgentProperties.SetId(button, "lab.recovery");
AgentProperties.SetRole(button, AgentRoleNames.Button);
```

Enable with env `NOVOLIS_AVALONIA_AGENT=1`. Optional endpoint override: `NOVOLIS_AVALONIA_AGENT_ENDPOINT`.

## Methods

| Method | Purpose |
|--------|---------|
| `ui.hello` | Handshake |
| `ui.tree` | Interactive control dump |
| `ui.screenshot` | Window/control PNG |
| `ui.click` | Click by id or coordinates |
| `ui.type` | Type/replace text + special keys (`Clear` replaces TextBox contents) |
| `ui.select` | Select ListBox / ComboBox / TabControl by index or item text |
| `ui.wait` | Wait for control conditions |
