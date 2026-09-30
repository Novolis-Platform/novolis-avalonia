using MessagePack;

namespace Novolis.Avalonia.Agent.Protocol.Dto;

[MessagePackObject]
public sealed record UiGetRequestDto(
    [property: Key(0)] long RequestId,
    [property: Key(1)] string[] ControlIds);
