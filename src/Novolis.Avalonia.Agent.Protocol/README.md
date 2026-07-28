# Novolis.Avalonia.Agent.Protocol

MessagePack DTOs, LocalIpc framing helpers, and `UiAgentClient` for the Avalonia UI agent protocol (`ui.hello`, `ui.tree`, `ui.screenshot`, `ui.click`, `ui.type`, `ui.select`, `ui.wait`).

## Install

```bash
dotnet add package Novolis.Avalonia.Agent.Protocol
```

## Endpoint

Default named pipe `novolis-avalonia-agent` (Windows) or temp socket `novolis-avalonia-agent.sock`. Override with env `NOVOLIS_AVALONIA_AGENT_ENDPOINT`.

## Quick start

```csharp
await using var client = new UiAgentClient();
await client.ConnectAsync(UiTransportEndpoints.CreateDefault());
var hello = await client.HelloAsync();
var tree = await client.TreeAsync();
```
