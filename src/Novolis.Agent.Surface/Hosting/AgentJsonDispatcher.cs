using System.Text.Json;

namespace Novolis.Agent.Surface;

public static class AgentJsonDispatcher
{
    public static readonly JsonSerializerOptions JsonOptions = AgentJson.Options;

    public static AgentCommandDto ParseCommand(JsonElement root)
    {
        return root.Deserialize<AgentCommandDto>(JsonOptions) ?? new AgentCommandDto();
    }

    public static object Dispatch(IAgentSession session, string? method, JsonElement root)
    {
        ArgumentNullException.ThrowIfNull(session);
        return method switch
        {
            AgentMethodNames.Hello or "hello" => session.Hello(),
            AgentMethodNames.Snapshot or "snapshot" => session.Snapshot(),
            AgentMethodNames.Actions or "actions" => session.Actions(),
            AgentMethodNames.Subscribe or "subscribe" => Subscribe(session),
            AgentMethodNames.Command or "command" => session.Execute(ParseCommand(
                root.TryGetProperty("command", out var c) ? c :
                root.TryGetProperty("params", out var p) ? p : root)),
            _ => throw new InvalidOperationException($"Unknown method '{method}'."),
        };
    }

    private static AgentSubscribeResponseDto Subscribe(IAgentSession session)
    {
        session.Subscribe();
        return new AgentSubscribeResponseDto { Ok = true };
    }
}
