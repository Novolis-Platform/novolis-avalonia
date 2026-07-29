using System.Text.Json;

namespace Novolis.Avalonia.Cad.Session;

public static class CadSessionJsonDispatcher
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public static object Dispatch(ICadSession session, string? method, JsonElement root)
    {
        ArgumentNullException.ThrowIfNull(session);
        return method switch
        {
            CadSessionMethodNames.Hello or "hello" => session.Hello(),
            CadSessionMethodNames.Snapshot or "snapshot" => session.Snapshot(),
            CadSessionMethodNames.Actions or "actions" => session.Actions(),
            CadSessionMethodNames.Subscribe or "subscribe" => Subscribe(session),
            CadSessionMethodNames.Command or "command" => session.Execute(ParseCommand(root)),
            _ => throw new InvalidOperationException($"Unknown method '{method}'"),
        };
    }

    public static CadCommandDto ParseCommand(JsonElement root)
    {
        var cmd = new CadCommandDto();
        JsonElement nested = default;
        var hasNested = root.ValueKind == JsonValueKind.Object
                        && (root.TryGetProperty("command", out nested) || root.TryGetProperty("args", out nested));
        var source = hasNested ? nested : root;
        if (source.ValueKind != JsonValueKind.Object)
            return cmd;

        if (TryString(source, "actionId", out var actionId))
            cmd.ActionId = actionId ?? "";
        if (TryString(source, "path", out var path))
            cmd.Path = path;
        if (TryString(source, "tool", out var tool))
            cmd.Tool = tool;
        if (TryString(source, "viewMode", out var viewMode))
            cmd.ViewMode = viewMode;
        if (TryString(source, "prompt", out var prompt))
            cmd.Prompt = prompt;
        if (TryString(source, "kind", out var kind))
            cmd.Kind = kind;
        if (TryString(source, "exportRoot", out var exportRoot))
            cmd.ExportRoot = exportRoot;
        if (TryGuid(source, "entityId", out var entityId))
            cmd.EntityId = entityId;
        if (TryFloat(source, "elevation", out var elevation))
            cmd.Elevation = elevation;
        if (TryFloat(source, "gridStep", out var grid))
            cmd.GridStep = grid;
        if (TryBool(source, "snap", out var snap))
            cmd.Snap = snap;
        return cmd;
    }

    private static CadSubscribeResponseDto Subscribe(ICadSession session)
    {
        session.Subscribe();
        return new CadSubscribeResponseDto { Ok = true };
    }

    private static bool TryString(JsonElement el, string name, out string? value)
    {
        if (el.TryGetProperty(name, out var p))
        {
            value = p.GetString();
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryFloat(JsonElement el, string name, out float value)
    {
        if (el.TryGetProperty(name, out var p) && p.TryGetSingle(out value))
            return true;
        value = 0;
        return false;
    }

    private static bool TryBool(JsonElement el, string name, out bool value)
    {
        if (el.TryGetProperty(name, out var p) && p.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            value = p.GetBoolean();
            return true;
        }

        value = false;
        return false;
    }

    private static bool TryGuid(JsonElement el, string name, out Guid value)
    {
        if (el.TryGetProperty(name, out var p) && Guid.TryParse(p.GetString(), out value))
            return true;
        value = default;
        return false;
    }
}
