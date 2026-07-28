using MessagePack;

namespace Novolis.Avalonia.Agent.Protocol.Dto;

[MessagePackObject]
public sealed record UiClickRequestDto(
    [property: Key(0)] long RequestId,
    [property: Key(1)] string? ControlId,
    [property: Key(2)] double? X,
    [property: Key(3)] double? Y);
