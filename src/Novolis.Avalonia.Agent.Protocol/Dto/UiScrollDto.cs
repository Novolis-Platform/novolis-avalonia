using MessagePack;

namespace Novolis.Avalonia.Agent.Protocol.Dto;

[MessagePackObject]
public sealed record UiScrollRequestDto(
    [property: Key(0)] long RequestId,
    [property: Key(1)] string? ControlId,
    [property: Key(2)] double? DeltaX,
    [property: Key(3)] double? DeltaY,
    [property: Key(4)] bool BringIntoView = false);
