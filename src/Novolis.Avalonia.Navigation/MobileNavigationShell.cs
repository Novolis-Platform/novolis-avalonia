using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Novolis.Avalonia.GraphicalProfile;
using Novolis.Commands.Navigation;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;

namespace Novolis.Avalonia.Navigation;

/// <summary>
/// Native Avalonia mobile shell for a shared semantic menu tree.
/// </summary>
public sealed class MobileNavigationShell : Grid
{
    readonly MobileNavigationController _navigation;
    readonly Grid _header = new();
    readonly Grid _primaryBar = new();
    readonly ContentControl _body = new();
    readonly Grid _overlayLayer = new();
    readonly Border _overlayCard = new();
    readonly StackPanel _menuItems = new() { Spacing = GraphicalProfileColors.ControlGap };
    readonly TextBlock _title = new();
    readonly TextBlock _overlayTitle = new();
    readonly Button _backButton;
    readonly Button _menuButton;

    /// <summary>Creates an Avalonia shell over a validated navigation controller.</summary>
    public MobileNavigationShell(MobileNavigationController navigation)
    {
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        SetValue(AutomationProperties.AutomationIdProperty, "MobileNavigationShell");
        _backButton = CreateChromeButton("Back", "MobileNavigationBack", Back);
        _menuButton = CreateChromeButton("More", "MobileNavigationMore", OpenMenu);
        Build();
        _navigation.Changed += OnNavigationChanged;
        _navigation.Activated += OnNavigationActivated;
        ApplyTheme();
        Refresh();
    }

    /// <summary>Raised after a route or command node is activated.</summary>
    public event EventHandler<MobileMenuActivationEventArgs>? ActivationRequested;

    /// <summary>Raised after the shared navigation state changes.</summary>
    public event EventHandler<MobileNavigationChangedEventArgs>? NavigationChanged;

    /// <summary>Current shared navigation controller.</summary>
    public MobileNavigationController Navigation => _navigation;

    /// <summary>Title shown in the native app bar.</summary>
    public string Title
    {
        get => _title.Text ?? string.Empty;
        set => _title.Text = value ?? string.Empty;
    }

    /// <summary>Route content owned by the host application.</summary>
    public Control? Body
    {
        get => _body.Content as Control;
        set => _body.Content = value;
    }

    /// <summary>Applies current Graphical Profile resources to the shell.</summary>
    public void ApplyTheme()
    {
        BindBrush(this, BackgroundProperty, Profile.BackgroundResourceKey, Profile.BackgroundBrush);
        BindBrush(_header, Panel.BackgroundProperty, Profile.SurfaceResourceKey, Profile.SurfaceBrush);
        BindBrush(_overlayCard, Border.BackgroundProperty, Profile.SurfaceResourceKey, Profile.SurfaceBrush);
        BindBrush(_overlayCard, Border.BorderBrushProperty, Profile.BorderResourceKey, Profile.BorderBrush);
        BindBrush(_title, TextBlock.ForegroundProperty, Profile.TextResourceKey, Profile.TextBrush);
        BindBrush(
            _overlayTitle,
            TextBlock.ForegroundProperty,
            Profile.TextResourceKey,
            Profile.TextBrush);
        RenderPrimaryBar();
        RenderMenu();
    }

    /// <summary>Lets a host forward Android or hardware Back.</summary>
    public bool TryHandleBack() => _navigation.Back();

    /// <summary>Opens the secondary menu surface.</summary>
    public bool OpenSecondaryMenu() => _navigation.OpenSecondaryMenu();

    void Build()
    {
        RowDefinitions = new RowDefinitions
        {
            new(GridLength.Auto),
            new(GridLength.Star),
            new(GridLength.Auto),
        };

        _header.ColumnDefinitions = new ColumnDefinitions
        {
            new(GridLength.Auto),
            new(GridLength.Star),
            new(GridLength.Auto),
        };
        _header.ColumnSpacing = GraphicalProfileColors.ControlGap;
        _header.Margin = new Thickness(12, 8, 12, 6);
        _header.Children.Add(_backButton);
        Grid.SetColumn(_backButton, 0);
        _header.Children.Add(_title);
        Grid.SetColumn(_title, 1);
        _header.Children.Add(_menuButton);
        Grid.SetColumn(_menuButton, 2);
        _title.FontFamily = Profile.BodyFont;
        _title.FontSize = GraphicalProfileColors.IntervalTitleSize;
        _title.FontWeight = FontWeight.Bold;
        _title.VerticalAlignment = VerticalAlignment.Center;
        _title.TextTrimming = TextTrimming.CharacterEllipsis;

        _body.HorizontalAlignment = HorizontalAlignment.Stretch;
        _body.VerticalAlignment = VerticalAlignment.Stretch;

        _primaryBar.ColumnSpacing = GraphicalProfileColors.ControlGap;
        _primaryBar.Margin = new Thickness(8, 6);
        _primaryBar.MinHeight = GraphicalProfileColors.IconControl;

        var overlayHeader = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions
            {
                new(GridLength.Star),
                new(GridLength.Auto),
            },
            ColumnSpacing = GraphicalProfileColors.ControlGap,
        };
        overlayHeader.Children.Add(_overlayTitle);
        Grid.SetColumn(_overlayTitle, 0);
        var close = CreateChromeButton(
            "Close",
            "MobileNavigationClose",
            () => _navigation.CloseOverlay());
        overlayHeader.Children.Add(close);
        Grid.SetColumn(close, 1);
        _overlayTitle.Text = "Menu";
        _overlayTitle.FontFamily = Profile.BodyFont;
        _overlayTitle.FontSize = GraphicalProfileColors.SectionTitleSize;
        _overlayTitle.FontWeight = FontWeight.Bold;
        _overlayTitle.VerticalAlignment = VerticalAlignment.Center;

        _overlayCard.Padding = new Thickness(
            GraphicalProfileColors.CardPadding,
            GraphicalProfileColors.CardPadding,
            GraphicalProfileColors.CardPadding,
            GraphicalProfileColors.PageInset);
        _overlayCard.BorderThickness = new Thickness(GraphicalProfileColors.Stroke);
        _overlayCard.CornerRadius = new CornerRadius(GraphicalProfileColors.CardRadius);
        _overlayCard.Child = new StackPanel
        {
            Spacing = GraphicalProfileColors.SectionGap,
            Children =
            {
                overlayHeader,
                new ScrollViewer
                {
                    MaxHeight = 480,
                    VerticalScrollBarVisibility =
                        global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                    Content = _menuItems,
                },
            },
        };
        _overlayCard.HorizontalAlignment = HorizontalAlignment.Stretch;
        _overlayCard.VerticalAlignment = VerticalAlignment.Bottom;

        var scrim = CreateChromeButton(
            string.Empty,
            "MobileNavigationScrim",
            () => _navigation.CloseOverlay());
        scrim.Background = Brushes.Transparent;
        scrim.BorderBrush = Brushes.Transparent;
        scrim.BorderThickness = new Thickness(0);
        scrim.SetValue(AutomationProperties.NameProperty, "Close menu");
        _overlayLayer.Children.Add(scrim);
        _overlayLayer.Children.Add(_overlayCard);
        _overlayLayer.IsVisible = false;
        _overlayLayer.HorizontalAlignment = HorizontalAlignment.Stretch;
        _overlayLayer.VerticalAlignment = VerticalAlignment.Stretch;

        Children.Add(_header);
        Grid.SetRow(_header, 0);
        Children.Add(_body);
        Grid.SetRow(_body, 1);
        Children.Add(_primaryBar);
        Grid.SetRow(_primaryBar, 2);
        Children.Add(_overlayLayer);
        Grid.SetRowSpan(_overlayLayer, 3);
    }

    void Refresh()
    {
        _backButton.IsVisible =
            _navigation.State.Overlay != MobileMenuOverlay.None
            || _navigation.State.RouteStack.Count > 1;
        _menuButton.IsVisible = _navigation.GetSecondaryNodes()
            .Any(node => node.Kind != MobileMenuNodeKind.Separator);
        _overlayLayer.IsVisible = _navigation.State.Overlay != MobileMenuOverlay.None;
        RenderPrimaryBar();
        RenderMenu();
    }

    void RenderPrimaryBar()
    {
        _primaryBar.Children.Clear();
        _primaryBar.ColumnDefinitions.Clear();
        var nodes = _navigation.GetPrimaryNodes();
        _primaryBar.IsVisible = nodes.Count > 0;
        for (var index = 0; index < nodes.Count; index++)
        {
            _primaryBar.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var node = nodes[index];
            var button = CreateNodeButton(node, $"MobileNavigationPrimary.{node.Id}");
            _primaryBar.Children.Add(button);
            Grid.SetColumn(button, index);
        }
    }

    void RenderMenu()
    {
        _menuItems.Children.Clear();
        if (!_overlayLayer.IsVisible)
            return;

        var current = _navigation.State.CurrentMenuNodeId;
        _overlayTitle.Text = current is null
            ? "Menu"
            : _navigation.Tree.Find(current)?.Label ?? "Menu";
        if (_navigation.State.MenuPath.Count > 0)
        {
            _menuItems.Children.Add(
                CreateChromeButton(
                    "Back",
                    "MobileNavigationMenuBack",
                    () => _navigation.Back()));
        }

        var nodes = _navigation.GetOpenMenuNodes();
        if (nodes.Count == 0)
        {
            _menuItems.Children.Add(
                new TextBlock
                {
                    Text = "No additional actions",
                    FontFamily = Profile.BodyFont,
                    FontSize = GraphicalProfileColors.BodySize,
                    Foreground = Profile.MutedBrush,
                    TextWrapping = TextWrapping.Wrap,
                });
            return;
        }

        foreach (var node in nodes)
        {
            if (node.Kind == MobileMenuNodeKind.Separator)
            {
                _menuItems.Children.Add(
                    new Border
                    {
                        Height = GraphicalProfileColors.Stroke,
                        Background = Profile.BorderBrush,
                    });
                continue;
            }

            _menuItems.Children.Add(
                CreateNodeButton(node, $"MobileNavigationMenu.{node.Id}"));
        }
    }

    Button CreateNodeButton(MobileMenuNode node, string automationId)
    {
        var label = node.Label;
        if (!string.IsNullOrWhiteSpace(node.Subtitle))
            label += $"\n{node.Subtitle}";
        if (node.Kind == MobileMenuNodeKind.Group)
            label += "  ›";
        if (node.Kind == MobileMenuNodeKind.Toggle)
            label += node.IsChecked ? "  On" : "  Off";

        var button = CreateChromeButton(label, automationId, () => _navigation.Activate(node.Id));
        button.HorizontalContentAlignment = HorizontalAlignment.Left;
        button.IsEnabled = node.IsEnabled;
        BindBrush(
            button,
            Button.BackgroundProperty,
            node.IsSelected ? Profile.AccentFillResourceKey : Profile.RaisedResourceKey,
            node.IsSelected ? Profile.AccentFillBrush : Profile.RaisedBrush);
        BindBrush(
            button,
            Button.ForegroundProperty,
            node.IsSelected ? Profile.OnAccentFillResourceKey : Profile.TextResourceKey,
            node.IsSelected ? Profile.OnAccentFillBrush : Profile.TextBrush);
        button.SetValue(
            AutomationProperties.NameProperty,
            node.IsEnabled
                ? node.Label
                : $"{node.Label}. {node.DisabledReason ?? "Unavailable"}");
        return button;
    }

    Button CreateChromeButton(string text, string automationId, Func<bool> action)
    {
        var button = new Button
        {
            Content = text,
            FontFamily = Profile.BodyFont,
            FontSize = GraphicalProfileColors.CaptionSize,
            FontWeight = FontWeight.Bold,
            MinHeight = GraphicalProfileColors.TouchTarget,
            MinWidth = GraphicalProfileColors.IconControl,
            Padding = new Thickness(10, 7),
            BorderThickness = new Thickness(GraphicalProfileColors.Stroke),
            CornerRadius = new CornerRadius(GraphicalProfileColors.ControlRadius),
        };
        button.SetValue(AutomationProperties.AutomationIdProperty, automationId);
        BindBrush(
            button,
            Button.BackgroundProperty,
            Profile.RaisedResourceKey,
            Profile.RaisedBrush);
        BindBrush(
            button,
            Button.ForegroundProperty,
            Profile.TextResourceKey,
            Profile.TextBrush);
        BindBrush(
            button,
            Button.BorderBrushProperty,
            Profile.BorderResourceKey,
            Profile.BorderBrush);
        button.Click += (_, _) => action();
        return button;
    }

    static void BindBrush(
        AvaloniaObject target,
        AvaloniaProperty property,
        string resourceKey,
        IBrush fallback)
    {
        if (Dispatcher.UIThread.CheckAccess())
            GraphicalProfileBinding.Bind(target, property, resourceKey);
        else
            target.SetValue(property, fallback);
    }

    bool Back() => _navigation.Back();

    bool OpenMenu() => _navigation.OpenSecondaryMenu();

    void OnNavigationChanged(
        object? sender,
        MobileNavigationChangedEventArgs args)
    {
        Refresh();
        NavigationChanged?.Invoke(this, args);
    }

    void OnNavigationActivated(
        object? sender,
        MobileMenuActivationEventArgs args) =>
        ActivationRequested?.Invoke(this, args);
}
