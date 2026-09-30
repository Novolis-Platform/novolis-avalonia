using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using Novolis.Chat.Abstractions;

namespace Novolis.Avalonia.Chat;

/// <summary>Presence dots and nicks bound to live chat presence DTOs.</summary>
public sealed class ChatPresenceList : Border
{
    public static readonly StyledProperty<IReadOnlyList<ChatPresence>> PresenceProperty =
        AvaloniaProperty.Register<ChatPresenceList, IReadOnlyList<ChatPresence>>(
            nameof(Presence),
            Array.Empty<ChatPresence>());

    readonly StackPanel _items = new() { Spacing = 6 };

    public ChatPresenceList()
    {
        Background = Profile.SurfaceBrush;
        BorderBrush = Profile.BorderBrush;
        BorderThickness = new Thickness(1);
        Padding = new Thickness(10);
        Child = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _items,
        };
        Rebuild();
    }

    public event EventHandler<string>? NickSelected;

    public IReadOnlyList<ChatPresence> Presence
    {
        get => GetValue(PresenceProperty);
        set => SetValue(PresenceProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PresenceProperty)
            Rebuild();
    }

    void Rebuild()
    {
        _items.Children.Clear();
        foreach (var entry in Presence.OrderBy(value => value.Nick, StringComparer.OrdinalIgnoreCase))
        {
            var fill = entry.Status switch
            {
                ChatPresenceStatus.Online => Profile.ActionSoftBrush,
                ChatPresenceStatus.Away => Profile.WarningBrush,
                _ => Profile.MutedBrush,
            };
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 7,
                Children =
                {
                    new Ellipse
                    {
                        Width = 8,
                        Height = 8,
                        Fill = fill,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                    new TextBlock
                    {
                        Text = entry.Nick,
                        Foreground = Profile.TextBrush,
                    },
                },
            };
            var button = new Button
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(2),
                Content = row,
            };
            button.Click += (_, _) => NickSelected?.Invoke(this, entry.Nick);
            _items.Children.Add(button);
        }
    }
}
