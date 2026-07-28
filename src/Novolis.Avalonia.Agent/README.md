# Novolis.Avalonia.Agent

Embeds a LocalIpc `ui.*` agent host in an Avalonia window so MCP / tooling can dump the visual tree, screenshot, click, and type.

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
