using Novolis.Chat.Abstractions;

namespace Novolis.Avalonia.Chat;

/// <summary>One selectable space/channel entry for the left rail.</summary>
public sealed record ChatRailItem(
    SpaceId SpaceId,
    string SpaceName,
    ChannelId Channel,
    int UnreadCount = 0);

/// <summary>One decrypted message projection for the thread view.</summary>
/// <remarks>The body exists only at the consumer boundary; transport frames never carry it.</remarks>
public sealed record ChatMessageDto(ChatFrame Frame, MarkdownBody Body);
