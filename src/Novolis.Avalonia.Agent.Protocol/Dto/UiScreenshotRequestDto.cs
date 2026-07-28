using MessagePack;

namespace Novolis.Avalonia.Agent.Protocol.Dto;

[MessagePackObject]
public sealed record UiScreenshotRequestDto(
    [property: Key(0)] long RequestId,
    [property: Key(1)] string? ControlId,
    [property: Key(2)] int? MaxWidth);
