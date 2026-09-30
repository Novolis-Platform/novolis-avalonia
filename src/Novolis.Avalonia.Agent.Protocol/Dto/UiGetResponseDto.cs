using MessagePack;

namespace Novolis.Avalonia.Agent.Protocol.Dto;

[MessagePackObject]
public sealed record UiGetResponseDto(
    [property: Key(0)] long RequestId,
    [property: Key(1)] bool Success,
    [property: Key(2)] string? Error,
    [property: Key(3)] UiControlStateDto[] Controls,
    [property: Key(4)] string? AppTitle,
    [property: Key(5)] int ProcessId);
