<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Agent.Protocol

MessagePack DTOs, LocalIpc framing helpers, and `UiAgentClient` for the Avalonia UI agent RPC protocol (`ui.hello`, `ui.tree`, `ui.screenshot`, `ui.click`, `ui.type`, `ui.select`, `ui.focus`, `ui.scroll`, `ui.wait`, …).

## Install

```bash
dotnet add package Novolis.Avalonia.Agent.Protocol
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Endpoint

Default named pipe `novolis-avalonia-agent` (Windows) or temp socket `novolis-avalonia-agent.sock` (Unix). Override with env `NOVOLIS_AVALONIA_AGENT_ENDPOINT`.

## Quick start

```csharp
using Novolis.Avalonia.Agent.Protocol;

await using var client = new UiAgentClient();
await client.ConnectDefaultAsync();
var hello = await client.HelloAsync();
var tree = await client.TreeAsync(interactiveOnly: true);
var shot = await client.ScreenshotAsync(maxWidth: 1280);
```

## API

| API | Purpose |
|-----|---------|
| `UiAgentClient` | Async RPC client over `ILocalIpcClient` |
| `UiAgentClient.ConnectAsync(endpoint)` / `ConnectDefaultAsync()` | Connect to agent host |
| `UiAgentClient.HelloAsync()` | Handshake; returns `UiHelloResponseDto` |
| `UiAgentClient.TreeAsync(interactiveOnly)` | Accessibility/control tree |
| `UiAgentClient.ScreenshotAsync(controlId?, maxWidth?)` | PNG screenshot |
| `UiAgentClient.ClickAsync(controlId?, x?, y?, button?, clickCount)` | Click by id or coordinates |
| `UiAgentClient.TypeAsync(controlId?, text?, keys?, clear)` | Type text or key chords |
| `UiAgentClient.SelectAsync(controlId, index?, itemText?)` | List/combo selection |
| `UiAgentClient.FocusAsync(controlId)` | Focus a control |
| `UiAgentClient.ScrollAsync(controlId?, deltaX?, deltaY?, bringIntoView)` | Scroll / bring into view |
| `UiAgentClient.WaitAsync(controlId, enabled?, textContains?, timeoutMs)` | Wait for control state |
| `UiAgentClient.GetAsync(controlIds)` | Batch read control state |
| `UiAgentClient.ItemsAsync(controlId)` | List items for list/combo controls |
| `UiTransportEndpoints.CreateDefault()` | Platform default pipe/socket endpoint |
| `UiProtocolCodec.Serialize<T>` / `Deserialize<T>` | MessagePack codec |
| `UiProtocolVersion.Current` | Protocol version string (`"1.2"`) |
| `UiRpcMethodNames.*` | RPC method constants (`ui.hello`, `ui.tree`, …) |
| `AgentRoleNames.*` | Semantic roles (`button`, `textbox`, `listbox`, …) |
| `AgentIdAttribute` | Marks controls with stable agent ids |
| `UiTreeNodeDto` / `UiBoundsDto` / `UiControlStateDto` / `UiItemDto` | Core DTO records |

## Related / dogfood

| Package / app | Notes |
|---------------|-------|
| [`Novolis.Avalonia.Agent`](../Novolis.Avalonia.Agent/README.md) | Host side — embed agent in Avalonia window |
| [AvaloniaAgentMcp](../../../novolis-dogfooding/apps/AvaloniaAgentMcp) | MCP bridge over the UI agent protocol |

