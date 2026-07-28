using MessagePack;

namespace Novolis.Avalonia.Agent.Protocol.Dto;

[MessagePackObject]
public sealed record UiHelloRequestDto(
    [property: Key(0)] long RequestId);
