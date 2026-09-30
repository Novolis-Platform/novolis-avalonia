using Novolis.Chat.Abstractions;

namespace Novolis.Avalonia.Chat;

/// <summary>One selectable space/channel entry for the left rail.</summary>
public sealed record ChatRailItem(
    SpaceId SpaceId,
    string SpaceName,
    ChannelId Channel,
    int UnreadCount = 0);
