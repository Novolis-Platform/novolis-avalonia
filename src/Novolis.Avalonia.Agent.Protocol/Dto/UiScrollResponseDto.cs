using MessagePack;

namespace Novolis.Avalonia.Agent.Protocol.Dto;

[MessagePackObject]
public sealed record UiScrollResponseDto(
    [property: Key(0)] long RequestId,
    [property: Key(1)] bool Success,
    [property: Key(2)] string? Error,
    [property: Key(3)] double? OffsetX,
    [property: Key(4)] double? OffsetY);
