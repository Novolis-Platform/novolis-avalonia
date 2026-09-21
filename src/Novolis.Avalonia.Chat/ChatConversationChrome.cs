using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Chat.Abstractions;

namespace Novolis.Avalonia.Chat;

/// <summary>Small code-first composition of rail, Markdown thread, and presence.</summary>
public sealed class ChatConversationChrome : Grid
{
    public ChatConversationChrome()
    {
        ColumnDefinitions = new ColumnDefinitions("190,*,190");
        Background = new SolidColorBrush(Color.Parse("#0a1422"));

        Rail = new ChatRail();
        Thread = new ChatThreadPanel();
        Presence = new ChatPresenceList();

        Children.Add(Rail);
        Grid.SetColumn(Thread, 1);
        Children.Add(Thread);
        Grid.SetColumn(Presence, 2);
        Presence.Margin = new Thickness(8, 0, 0, 0);
        Children.Add(Presence);
    }

    public ChatRail Rail { get; }

    public ChatThreadPanel Thread { get; }

    public ChatPresenceList Presence { get; }

    public void SetState(
        IReadOnlyList<ChatRailItem> rail,
        IReadOnlyList<ChatMessageDto> messages,
        IReadOnlyList<ChatPresence> presence)
    {
        Rail.Items = rail;
        Thread.Messages = messages;
        Presence.Presence = presence;
    }
}
