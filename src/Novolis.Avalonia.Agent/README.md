# Novolis.Avalonia.Agent

Embeds a LocalIpc `ui.*` agent host in an Avalonia window so MCP / tooling can dump the visual tree, screenshot, click, type, select list/tab items, focus, and scroll.

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

// Dedicated pipe (when multiple Avalonia apps may run):
AgentHost.Attach(desktop.MainWindow, "novolis-avalonia-agent-myapp");

// Tag controls for stable ids:
AgentProperties.SetId(button, "lab.recovery");
AgentProperties.SetRole(button, AgentRoleNames.Button);
```

## Methods

| Method | Purpose |
|--------|---------|
| `ui.hello` | Handshake |
| `ui.get` | Compact multi-id read (text/enabled/visible) |
| `ui.items` | ListBox / ComboBox / TabControl item dump |
| `ui.tree` | Interactive control dump (agent lists emit item rows) |
| `ui.screenshot` | Window/control PNG |
| `ui.click` | Click by id or coordinates (`button`: left/right/middle; `clickCount` for double-click) |
| `ui.type` | Type/replace text + special keys (`Clear` replaces TextBox contents) |
| `ui.select` | Select ListBox / ComboBox / TabControl by index or item text |
| `ui.focus` | Focus a control by id |
| `ui.scroll` | Scroll nearest `ScrollViewer` by delta, or `bringIntoView` |
| `ui.wait` | Host-side wait (blocks UI thread — prefer MCP `ui_poll` while simulating) |

Protocol version: `1.2`. Unknown methods return a typed fault (clients do not hang).

## Crash guard

```csharp
CrashGuard.Install("MyApp");
// After Avalonia setup:
CrashGuard.InstallAvalonia(Dispatcher.UIThread);
```

Unhandled UI / task faults write `%LocalAppData%/Novolis/<app>/crashes/crash-*.log` (+ `.dmp` on fatal
Windows process faults), open **Notepad** once, and mark dispatcher exceptions handled so the window
stays up. Agent IPC faults are logged silently (`ReportSilent`) without killing the host.

Enable with env `NOVOLIS_AVALONIA_AGENT=1`. Optional endpoint override: `NOVOLIS_AVALONIA_AGENT_ENDPOINT`.
