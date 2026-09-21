using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Markdown;
using Novolis.Chat.Abstractions;

namespace Novolis.Avalonia.Chat;

/// <summary>
/// Thread surface that renders decrypted Markdown sources while keeping the
/// associated public ChatFrame available for thread and receipt actions.
/// </summary>
public sealed class ChatThreadPanel : Border
{
    public static readonly StyledProperty<IReadOnlyList<ChatMessageDto>> MessagesProperty =
        AvaloniaProperty.Register<ChatThreadPanel, IReadOnlyList<ChatMessageDto>>(
            nameof(Messages),
            Array.Empty<ChatMessageDto>());

    readonly StackPanel _items = new() { Spacing = 8 };

    public ChatThreadPanel()
    {
        Background = new SolidColorBrush(Color.Parse("#0f1c2e"));
        Padding = new Thickness(12);
        Child = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _items,
        };
        Rebuild();
    }

    public event EventHandler<ChatMessageDto>? MessageSelected;

    public IReadOnlyList<ChatMessageDto> Messages
    {
        get => GetValue(MessagesProperty);
        set => SetValue(MessagesProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MessagesProperty)
            Rebuild();
    }

    void Rebuild()
    {
        _items.Children.Clear();
        foreach (var message in Messages)
        {
            var preview = new MarkdownPreviewPane
            {
                Markdown = message.Body.Source,
                PreviewTheme = MarkdownPreviewTheme.StudioDark,
                MinHeight = 24,
                MaxHeight = 800,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            var header = new Button
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(0),
                Content = new TextBlock
                {
                    Text = FormatHeader(message.Frame),
                    Foreground = new SolidColorBrush(Color.Parse("#c9853a")),
                    FontSize = 12,
                },
            };
            header.Click += (_, _) => MessageSelected?.Invoke(this, message);
            _items.Children.Add(new Border
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#2a415c")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 0, 0, 8),
                Child = new StackPanel
                {
                    Spacing = 4,
                    Children = { header, preview },
                },
            });
        }
    }

    static string FormatHeader(ChatFrame frame)
    {
        var threadText = frame.Thread is { } threadId ? $" · thread {threadId.Value:N}" : string.Empty;
        var reaction = string.IsNullOrWhiteSpace(frame.Reaction) ? string.Empty : $" · {frame.Reaction}";
        return $"<{frame.FromNick}> {frame.SentAtUtc.ToLocalTime():HH:mm}{threadText}{reaction}";
    }
}
