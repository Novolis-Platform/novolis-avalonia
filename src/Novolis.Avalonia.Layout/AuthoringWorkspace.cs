using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Novolis.Avalonia.Layout;

/// <summary>
/// Adaptive authoring shell: Wide (nav | primary | context) or Narrow (page-cycled regions).
/// Optional top and status bars. Width under <see cref="NarrowWidthThreshold"/> selects Narrow
/// unless <see cref="ForceMode"/> locks <see cref="LayoutMode"/>.
/// </summary>
public sealed class AuthoringWorkspace : Grid
{
    /// <summary>Default width below which adaptive mode switches to Narrow.</summary>
    public const double DefaultNarrowWidthThreshold = 900;

    readonly ContentControl _topBarHost = new()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch,
        IsVisible = false,
    };
    readonly ContentControl _statusBarHost = new()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch,
        IsVisible = false,
    };
    readonly ContentControl _navHost = new()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch,
    };
    readonly ContentControl _primaryHost = new()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch,
    };
    readonly ContentControl _contextHost = new()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch,
    };
    readonly ColumnDefinition _navColumn = new(new GridLength(280));
    readonly ColumnDefinition _primaryColumn = new(GridLength.Star);
    readonly ColumnDefinition _contextColumn = new(new GridLength(320));
    readonly RowDefinition _topRow = new(GridLength.Auto);
    readonly RowDefinition _statusRow = new(GridLength.Auto);

    /// <summary>Active layout mode (Wide or Narrow).</summary>
    public static readonly StyledProperty<AuthoringLayoutMode> LayoutModeProperty =
        AvaloniaProperty.Register<AuthoringWorkspace, AuthoringLayoutMode>(
            nameof(LayoutMode),
            AuthoringLayoutMode.Wide);

    /// <summary>When true, <see cref="LayoutMode"/> is not updated from width.</summary>
    public static readonly StyledProperty<bool> ForceModeProperty =
        AvaloniaProperty.Register<AuthoringWorkspace, bool>(nameof(ForceMode));

    /// <summary>Width below which adaptive mode selects Narrow (ignored when <see cref="ForceMode"/>).</summary>
    public static readonly StyledProperty<double> NarrowWidthThresholdProperty =
        AvaloniaProperty.Register<AuthoringWorkspace, double>(
            nameof(NarrowWidthThreshold),
            DefaultNarrowWidthThreshold);

    /// <summary>Region shown in Narrow mode (ignored in Wide).</summary>
    public static readonly StyledProperty<AuthoringRegion> VisibleRegionProperty =
        AvaloniaProperty.Register<AuthoringWorkspace, AuthoringRegion>(
            nameof(VisibleRegion),
            AuthoringRegion.Primary);

    /// <summary>Fixed width of the nav column in Wide mode.</summary>
    public static readonly StyledProperty<double> NavWidthProperty =
        AvaloniaProperty.Register<AuthoringWorkspace, double>(nameof(NavWidth), 280);

    /// <summary>Fixed width of the context column in Wide mode when context is present.</summary>
    public static readonly StyledProperty<double> ContextWidthProperty =
        AvaloniaProperty.Register<AuthoringWorkspace, double>(nameof(ContextWidth), 320);

    /// <summary>Left / first-page region content.</summary>
    public static readonly StyledProperty<Control?> NavProperty =
        AvaloniaProperty.Register<AuthoringWorkspace, Control?>(nameof(Nav));

    /// <summary>Center / main region content.</summary>
    public static readonly StyledProperty<Control?> PrimaryProperty =
        AvaloniaProperty.Register<AuthoringWorkspace, Control?>(nameof(Primary));

    /// <summary>Right / optional region content.</summary>
    public static readonly StyledProperty<Control?> ContextProperty =
        AvaloniaProperty.Register<AuthoringWorkspace, Control?>(nameof(Context));

    /// <summary>Optional top command / chrome bar.</summary>
    public static readonly StyledProperty<Control?> TopBarProperty =
        AvaloniaProperty.Register<AuthoringWorkspace, Control?>(nameof(TopBar));

    /// <summary>Optional bottom status bar.</summary>
    public static readonly StyledProperty<Control?> StatusBarProperty =
        AvaloniaProperty.Register<AuthoringWorkspace, Control?>(nameof(StatusBar));

    static AuthoringWorkspace()
    {
        LayoutModeProperty.Changed.AddClassHandler<AuthoringWorkspace>((s, _) => s.ApplyColumns());
        ForceModeProperty.Changed.AddClassHandler<AuthoringWorkspace>((s, _) => s.ApplyAdaptiveFromWidth());
        NarrowWidthThresholdProperty.Changed.AddClassHandler<AuthoringWorkspace>((s, _) => s.ApplyAdaptiveFromWidth());
        VisibleRegionProperty.Changed.AddClassHandler<AuthoringWorkspace>((s, _) => s.ApplyColumns());
        NavWidthProperty.Changed.AddClassHandler<AuthoringWorkspace>((s, _) => s.ApplyColumns());
        ContextWidthProperty.Changed.AddClassHandler<AuthoringWorkspace>((s, _) => s.ApplyColumns());
        NavProperty.Changed.AddClassHandler<AuthoringWorkspace>((s, e) => s._navHost.Content = e.NewValue as Control);
        PrimaryProperty.Changed.AddClassHandler<AuthoringWorkspace>((s, e) => s._primaryHost.Content = e.NewValue as Control);
        ContextProperty.Changed.AddClassHandler<AuthoringWorkspace>((s, e) =>
        {
            s._contextHost.Content = e.NewValue as Control;
            s.ApplyColumns();
        });
        TopBarProperty.Changed.AddClassHandler<AuthoringWorkspace>((s, e) =>
        {
            s._topBarHost.Content = e.NewValue as Control;
            s._topBarHost.IsVisible = e.NewValue is not null;
        });
        StatusBarProperty.Changed.AddClassHandler<AuthoringWorkspace>((s, e) =>
        {
            s._statusBarHost.Content = e.NewValue as Control;
            s._statusBarHost.IsVisible = e.NewValue is not null;
        });
    }

    /// <summary>Creates an empty adaptive authoring shell.</summary>
    public AuthoringWorkspace()
    {
        RowDefinitions = [_topRow, new RowDefinition(GridLength.Star), _statusRow];

        var columns = new Grid
        {
            ColumnDefinitions = [_navColumn, _primaryColumn, _contextColumn],
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        columns.Children.Add(_navHost);
        Grid.SetColumn(_navHost, 0);
        columns.Children.Add(_primaryHost);
        Grid.SetColumn(_primaryHost, 1);
        columns.Children.Add(_contextHost);
        Grid.SetColumn(_contextHost, 2);

        Children.Add(_topBarHost);
        Grid.SetRow(_topBarHost, 0);
        Children.Add(columns);
        Grid.SetRow(columns, 1);
        Children.Add(_statusBarHost);
        Grid.SetRow(_statusBarHost, 2);

        SizeChanged += (_, e) => ApplyAdaptiveFromWidth(e.NewSize.Width);
        ApplyColumns();
    }

    /// <summary>
    /// Creates an adaptive authoring shell with the given region content.
    /// </summary>
    public AuthoringWorkspace(
        Control? nav,
        Control? primary,
        Control? context = null,
        Control? topBar = null,
        Control? statusBar = null)
        : this()
    {
        Nav = nav;
        Primary = primary;
        Context = context;
        TopBar = topBar;
        StatusBar = statusBar;
    }

    /// <summary>Active layout mode (Wide or Narrow).</summary>
    public AuthoringLayoutMode LayoutMode
    {
        get => GetValue(LayoutModeProperty);
        set => SetValue(LayoutModeProperty, value);
    }

    /// <summary>When true, width changes do not alter <see cref="LayoutMode"/>.</summary>
    public bool ForceMode
    {
        get => GetValue(ForceModeProperty);
        set => SetValue(ForceModeProperty, value);
    }

    /// <summary>Width threshold for adaptive Narrow selection.</summary>
    public double NarrowWidthThreshold
    {
        get => GetValue(NarrowWidthThresholdProperty);
        set => SetValue(NarrowWidthThresholdProperty, value);
    }

    /// <summary>Region shown when <see cref="LayoutMode"/> is Narrow.</summary>
    public AuthoringRegion VisibleRegion
    {
        get => GetValue(VisibleRegionProperty);
        set => SetValue(VisibleRegionProperty, value);
    }

    /// <summary>Nav column width in Wide mode.</summary>
    public double NavWidth
    {
        get => GetValue(NavWidthProperty);
        set => SetValue(NavWidthProperty, value);
    }

    /// <summary>Context column width in Wide mode when context is present.</summary>
    public double ContextWidth
    {
        get => GetValue(ContextWidthProperty);
        set => SetValue(ContextWidthProperty, value);
    }

    /// <summary>Left / first-page region.</summary>
    public Control? Nav
    {
        get => GetValue(NavProperty);
        set => SetValue(NavProperty, value);
    }

    /// <summary>Center / main region.</summary>
    public Control? Primary
    {
        get => GetValue(PrimaryProperty);
        set => SetValue(PrimaryProperty, value);
    }

    /// <summary>Right / optional region.</summary>
    public Control? Context
    {
        get => GetValue(ContextProperty);
        set => SetValue(ContextProperty, value);
    }

    /// <summary>Optional top bar.</summary>
    public Control? TopBar
    {
        get => GetValue(TopBarProperty);
        set => SetValue(TopBarProperty, value);
    }

    /// <summary>Optional status bar.</summary>
    public Control? StatusBar
    {
        get => GetValue(StatusBarProperty);
        set => SetValue(StatusBarProperty, value);
    }

    /// <summary>Shows <paramref name="region"/> in Narrow mode (sets <see cref="VisibleRegion"/>).</summary>
    public void ShowRegion(AuthoringRegion region) => VisibleRegion = region;

    void ApplyAdaptiveFromWidth() => ApplyAdaptiveFromWidth(Bounds.Width);

    void ApplyAdaptiveFromWidth(double width)
    {
        if (ForceMode || width <= 0 || double.IsNaN(width))
            return;

        var next = width < NarrowWidthThreshold
            ? AuthoringLayoutMode.Narrow
            : AuthoringLayoutMode.Wide;
        if (LayoutMode != next)
            LayoutMode = next;
    }

    void ApplyColumns()
    {
        if (LayoutMode == AuthoringLayoutMode.Narrow)
        {
            ApplyNarrowColumns();
            return;
        }

        _navColumn.Width = new GridLength(Math.Max(0, NavWidth));
        _primaryColumn.Width = GridLength.Star;
        _contextColumn.Width = Context is null
            ? new GridLength(0)
            : new GridLength(Math.Max(0, ContextWidth));

        _navHost.IsVisible = true;
        _primaryHost.IsVisible = true;
        _contextHost.IsVisible = Context is not null;
    }

    void ApplyNarrowColumns()
    {
        var region = VisibleRegion;
        if (region == AuthoringRegion.Context && Context is null)
            region = AuthoringRegion.Primary;

        _navColumn.Width = region == AuthoringRegion.Nav ? GridLength.Star : new GridLength(0);
        _primaryColumn.Width = region == AuthoringRegion.Primary ? GridLength.Star : new GridLength(0);
        _contextColumn.Width = region == AuthoringRegion.Context ? GridLength.Star : new GridLength(0);

        _navHost.IsVisible = region == AuthoringRegion.Nav;
        _primaryHost.IsVisible = region == AuthoringRegion.Primary;
        _contextHost.IsVisible = region == AuthoringRegion.Context;
    }
}
