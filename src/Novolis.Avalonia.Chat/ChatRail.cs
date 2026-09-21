using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Chat.Abstractions;

namespace Novolis.Avalonia.Chat;

/// <summary>Code-first left rail for spaces, named channels, and unread counts.</summary>
public sealed class ChatRail : Border
{
    public static readonly StyledProperty<IReadOnlyList<ChatRailItem>> ItemsProperty =
        AvaloniaProperty.Register<ChatRail, IReadOnlyList<ChatRailItem>>(
            nameof(Items),
            Array.Empty<ChatRailItem>());

    readonly StackPanel _items = new() { Spacing = 4 };

    public ChatRail()
    {
        Background = new SolidColorBrush(Color.Parse("#102033"));
        BorderBrush = new SolidColorBrush(Color.Parse("#2a415c"));
        BorderThickness = new Thickness(0, 0, 1, 0);
        Padding = new Thickness(10);
        Width = 190;
        Child = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _items,
        };
        Rebuild();
    }

    public event EventHandler<ChatRailItem>? ChannelSelected;

    public IReadOnlyList<ChatRailItem> Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsProperty)
            Rebuild();
    }

    void Rebuild()
    {
        _items.Children.Clear();
        foreach (var item in Items)
        {
            var button = new Button
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 7),
                Content = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"{item.SpaceName}  {item.Channel.NormalizedName}",
                            Foreground = new SolidColorBrush(Color.Parse("#d9e4f0")),
                            TextTrimming = TextTrimming.CharacterEllipsis,
                        },
                        new TextBlock
                        {
                            Text = item.UnreadCount > 0 ? item.UnreadCount.ToString() : string.Empty,
                            Foreground = new SolidColorBrush(Color.Parse("#c9853a")),
                            HorizontalAlignment = HorizontalAlignment.Right,
                        },
                    },
                },
            };
            button.Click += (_, _) => ChannelSelected?.Invoke(this, item);
            _items.Children.Add(button);
        }
    }
}
