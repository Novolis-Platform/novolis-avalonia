# Novolis.Agent.Surface

Author an agent surface once; auto-construct the rest:

- Hello / action catalog / command JSON Schema
- OpenAPI 3 fragment for HTTP REST + SSE
- MCP tool descriptors
- Discovery env vars + `%TEMP%` markers
- Loopback **HTTP** (`HttpListener`) + **TCP JSONL** hosts

## Install

```bash
dotnet add package Novolis.Agent.Surface
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start

```csharp
[AgentSurface("scene", HttpPort = 18785, TcpPort = 18786,
    EnableEnv = "NOVOLIS_SCENE_SESSION", MarkerPrefix = "novolis-scene-session")]
public interface ISceneSession : IAgentSession { ... }

var def = AgentSurfaceDefinition.From<ISceneSession>();
await using var surface = AgentSurface.AttachAll(session, def);
```

Domain hosts stay out of `Novolis.Transports.*`.
