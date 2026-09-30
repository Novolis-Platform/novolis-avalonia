using MessagePack;

namespace Novolis.Avalonia.Agent.Protocol.Dto;

[MessagePackObject]
public sealed record UiItemDto(
    [property: Key(0)] int Index,
    [property: Key(1)] string Text,
    [property: Key(2)] bool Selected);
