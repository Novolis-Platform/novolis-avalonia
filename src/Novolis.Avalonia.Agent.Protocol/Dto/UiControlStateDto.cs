using MessagePack;

namespace Novolis.Avalonia.Agent.Protocol.Dto;

[MessagePackObject]
public sealed record UiControlStateDto(
    [property: Key(0)] string Id,
    [property: Key(1)] bool Found,
    [property: Key(2)] bool IsEnabled,
    [property: Key(3)] bool IsVisible,
    [property: Key(4)] string? Text,
    [property: Key(5)] string? Role,
    [property: Key(6)] string? TypeName);
