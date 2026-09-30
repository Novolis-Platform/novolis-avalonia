using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;

#pragma warning disable CS1591

namespace Novolis.Avalonia.GraphicalProfile;

/// <summary>
/// Installs the required Novolis graphical profile into an Avalonia
/// application.
/// </summary>
public static class GraphicalProfile
{
    public const string BackgroundResourceKey = "Ngp.Background";
    public const string SurfaceResourceKey = "Ngp.Surface";
    public const string RaisedResourceKey = "Ngp.Raised";
    public const string BorderResourceKey = "Ngp.Border";
    public const string TextResourceKey = "Ngp.Text";
    public const string MutedResourceKey = "Ngp.Muted";
    public const string AccentResourceKey = "Ngp.Accent";
    public const string AccentFillResourceKey = "Ngp.AccentFill";
    public const string OnAccentFillResourceKey = "Ngp.OnAccentFill";
    public const string ActionResourceKey = "Ngp.Action";
    public const string OnActionResourceKey = "Ngp.OnAction";
    public const string ActionSoftResourceKey = "Ngp.ActionSoft";
    public const string WarningResourceKey = "Ngp.Warning";
    public const string DangerResourceKey = "Ngp.Danger";

    public static FontFamily BodyFont { get; } = new(GraphicalProfileColors.FontFamily);

    public static FontFamily MonoFont { get; } = new(GraphicalProfileColors.MonoFontFamily);

    public static Color Background => CurrentColor(
        BackgroundResourceKey,
        GraphicalProfileColors.BackgroundDark);

    public static Color Surface => CurrentColor(
        SurfaceResourceKey,
        GraphicalProfileColors.SurfaceDark);

    public static Color Raised => CurrentColor(
        RaisedResourceKey,
        GraphicalProfileColors.RaisedDark);

    public static Color Border => CurrentColor(
        BorderResourceKey,
        GraphicalProfileColors.BorderDark);

    public static Color Text => CurrentColor(
        TextResourceKey,
        GraphicalProfileColors.TextDark);

    public static Color Muted => CurrentColor(
        MutedResourceKey,
        GraphicalProfileColors.MutedDark);

    public static Color Accent => CurrentColor(
        AccentResourceKey,
        GraphicalProfileColors.AccentDark);

    public static Color AccentFill => CurrentColor(
        AccentFillResourceKey,
        GraphicalProfileColors.AccentFillDark);

    public static Color OnAccentFill => CurrentColor(
        OnAccentFillResourceKey,
        GraphicalProfileColors.OnAccentFillDark);

    public static Color Action => CurrentColor(
        ActionResourceKey,
        GraphicalProfileColors.ActionDark);

    public static Color OnAction => CurrentColor(
        OnActionResourceKey,
        GraphicalProfileColors.OnActionDark);

    public static Color ActionSoft => CurrentColor(
        ActionSoftResourceKey,
        GraphicalProfileColors.ActionSoftDark);

    public static Color Warning => CurrentColor(
        WarningResourceKey,
        GraphicalProfileColors.WarningDark);

    public static Color Danger => CurrentColor(
        DangerResourceKey,
        GraphicalProfileColors.DangerDark);

    public static IBrush BackgroundBrush => new SolidColorBrush(Background);

    public static IBrush SurfaceBrush => new SolidColorBrush(Surface);

    public static IBrush RaisedBrush => new SolidColorBrush(Raised);

    public static IBrush BorderBrush => new SolidColorBrush(Border);

    public static IBrush TextBrush => new SolidColorBrush(Text);

    public static IBrush MutedBrush => new SolidColorBrush(Muted);

    public static IBrush AccentBrush => new SolidColorBrush(Accent);

    public static IBrush AccentFillBrush => new SolidColorBrush(AccentFill);

    public static IBrush OnAccentFillBrush => new SolidColorBrush(OnAccentFill);

    public static IBrush ActionBrush => new SolidColorBrush(Action);

    public static IBrush OnActionBrush => new SolidColorBrush(OnAction);

    public static IBrush ActionSoftBrush => new SolidColorBrush(ActionSoft);

    public static IBrush WarningBrush => new SolidColorBrush(Warning);

    public static IBrush DangerBrush => new SolidColorBrush(Danger);

    /// <summary>Installs theme resources and class-based chrome styles.</summary>
    public static void Install(Application application)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (application.Resources.TryGetResource(
                "Ngp.Installed",
                ThemeVariant.Default,
                out _))
        {
            return;
        }

        application.Resources.ThemeDictionaries[ThemeVariant.Light] =
            CreateThemeDictionary(light: true);
        application.Resources.ThemeDictionaries[ThemeVariant.Dark] =
            CreateThemeDictionary(light: false);
        application.Resources["Ngp.Installed"] = true;
        application.Styles.Add(CreateStyles());
    }

    /// <summary>Applies the current profile values to a window root.</summary>
    public static void ApplyWindowChrome(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        window.Background = BackgroundBrush;
        window.Foreground = TextBrush;
        window.FontFamily = BodyFont;
    }

    private static ResourceDictionary CreateThemeDictionary(bool light)
    {
        var dictionary = new ResourceDictionary();

        AddColor(dictionary, BackgroundResourceKey, light
            ? GraphicalProfileColors.BackgroundLight
            : GraphicalProfileColors.BackgroundDark);
        AddColor(dictionary, SurfaceResourceKey, light
            ? GraphicalProfileColors.SurfaceLight
            : GraphicalProfileColors.SurfaceDark);
        AddColor(dictionary, RaisedResourceKey, light
            ? GraphicalProfileColors.RaisedLight
            : GraphicalProfileColors.RaisedDark);
        AddColor(dictionary, BorderResourceKey, light
            ? GraphicalProfileColors.BorderLight
            : GraphicalProfileColors.BorderDark);
        AddColor(dictionary, TextResourceKey, light
            ? GraphicalProfileColors.TextLight
            : GraphicalProfileColors.TextDark);
        AddColor(dictionary, MutedResourceKey, light
            ? GraphicalProfileColors.MutedLight
            : GraphicalProfileColors.MutedDark);
        AddColor(dictionary, AccentResourceKey, light
            ? GraphicalProfileColors.AccentLight
            : GraphicalProfileColors.AccentDark);
        AddColor(dictionary, AccentFillResourceKey, light
            ? GraphicalProfileColors.AccentFillLight
            : GraphicalProfileColors.AccentFillDark);
        AddColor(dictionary, OnAccentFillResourceKey, light
            ? GraphicalProfileColors.OnAccentFillLight
            : GraphicalProfileColors.OnAccentFillDark);
        AddColor(dictionary, ActionResourceKey, light
            ? GraphicalProfileColors.ActionLight
            : GraphicalProfileColors.ActionDark);
        AddColor(dictionary, OnActionResourceKey, light
            ? GraphicalProfileColors.OnActionLight
            : GraphicalProfileColors.OnActionDark);
        AddColor(dictionary, ActionSoftResourceKey, light
            ? GraphicalProfileColors.ActionSoftLight
            : GraphicalProfileColors.ActionSoftDark);
        AddColor(dictionary, WarningResourceKey, light
            ? GraphicalProfileColors.WarningLight
            : GraphicalProfileColors.WarningDark);
        AddColor(dictionary, DangerResourceKey, light
            ? GraphicalProfileColors.DangerLight
            : GraphicalProfileColors.DangerDark);

        return dictionary;
    }

    private static void AddColor(ResourceDictionary dictionary, string key, string value)
    {
        var color = Color.Parse(value);
        dictionary[key] = color;
        dictionary[$"{key}Brush"] = new SolidColorBrush(color);
    }

    private static Color CurrentColor(string key, string fallback)
    {
        if (Application.Current is { } application
            && application.TryGetResource(
                key,
                application.ActualThemeVariant,
                out var value)
            && value is Color color)
        {
            return color;
        }

        return Color.Parse(fallback);
    }

    private static Styles CreateStyles() =>
        new()
        {
            TextStyle(
                "eyebrow",
                GraphicalProfileColors.EyebrowSize,
                FontWeight.Bold,
                GraphicalProfileColors.EyebrowTracking),
            TextStyle(
                "ngp-eyebrow",
                GraphicalProfileColors.EyebrowSize,
                FontWeight.Bold,
                GraphicalProfileColors.EyebrowTracking),
            TextStyle(
                "page-title",
                GraphicalProfileColors.PageTitleSize,
                FontWeight.Bold),
            TextStyle(
                "ngp-page-title",
                GraphicalProfileColors.PageTitleSize,
                FontWeight.Bold),
            TextStyle(
                "body-copy",
                GraphicalProfileColors.BodySize,
                FontWeight.Normal),
            TextStyle(
                "ngp-body",
                GraphicalProfileColors.BodySize,
                FontWeight.Normal),
            new Style(x => x.OfType<Button>().Class("nav-button"))
            {
                Setters =
                {
                    new Setter(
                        Button.MinHeightProperty,
                        GraphicalProfileColors.TouchTarget),
                    new Setter(
                        Button.PaddingProperty,
                        new Thickness(12, 7)),
                    new Setter(
                        Button.BackgroundProperty,
                        Resource("Ngp.SurfaceBrush")),
                    new Setter(
                        Button.ForegroundProperty,
                        Resource("Ngp.TextBrush")),
                    new Setter(
                        Button.BorderBrushProperty,
                        Resource("Ngp.BorderBrush")),
                    new Setter(
                        Button.BorderThicknessProperty,
                        new Thickness(GraphicalProfileColors.Stroke)),
                    new Setter(
                        Button.CornerRadiusProperty,
                        new CornerRadius(GraphicalProfileColors.BadgeRadius)),
                },
            },
            ButtonStyle("primary-button", "Ngp.AccentFillBrush", "Ngp.OnAccentFillBrush"),
            ButtonStyle("ngp-accent-button", "Ngp.AccentFillBrush", "Ngp.OnAccentFillBrush"),
            ButtonStyle("action-button", "Ngp.ActionBrush", "Ngp.OnActionBrush"),
            ButtonStyle("ngp-action-button", "Ngp.ActionBrush", "Ngp.OnActionBrush"),
            new Style(x => x.OfType<Border>().Class("ngp-card"))
            {
                Setters =
                {
                    new Setter(
                        global::Avalonia.Controls.Border.BackgroundProperty,
                        Resource("Ngp.SurfaceBrush")),
                    new Setter(
                        global::Avalonia.Controls.Border.BorderBrushProperty,
                        Resource("Ngp.BorderBrush")),
                    new Setter(
                        global::Avalonia.Controls.Border.BorderThicknessProperty,
                        new Thickness(GraphicalProfileColors.Stroke)),
                    new Setter(
                        global::Avalonia.Controls.Border.CornerRadiusProperty,
                        new CornerRadius(GraphicalProfileColors.CardRadius)),
                    new Setter(
                        global::Avalonia.Controls.Border.PaddingProperty,
                        new Thickness(GraphicalProfileColors.CardPadding)),
                },
            },
        };

    private static Style TextStyle(
        string className,
        double fontSize,
        FontWeight weight,
        double? letterSpacing = null)
    {
        var style = new Style(x => x.OfType<TextBlock>().Class(className))
        {
            Setters =
            {
                new Setter(TextBlock.FontFamilyProperty, BodyFont),
                new Setter(TextBlock.FontSizeProperty, fontSize),
                new Setter(TextBlock.FontWeightProperty, weight),
                new Setter(TextBlock.ForegroundProperty, Resource("Ngp.TextBrush")),
            },
        };

        if (letterSpacing is not null)
        {
            style.Setters.Add(
                new Setter(TextBlock.LetterSpacingProperty, letterSpacing.Value));
        }

        return style;
    }

    private static Style ButtonStyle(
        string className,
        string backgroundKey,
        string foregroundKey) =>
        new(x => x.OfType<Button>().Class(className))
        {
            Setters =
            {
                new Setter(
                    Button.MinHeightProperty,
                    GraphicalProfileColors.TouchTarget),
                new Setter(
                    Button.PaddingProperty,
                    new Thickness(16, 12)),
                new Setter(
                    Button.FontFamilyProperty,
                    BodyFont),
                new Setter(
                    Button.FontSizeProperty,
                    GraphicalProfileColors.ButtonSize),
                new Setter(
                    Button.FontWeightProperty,
                    FontWeight.Bold),
                new Setter(
                    Button.BackgroundProperty,
                    Resource(backgroundKey)),
                new Setter(
                    Button.ForegroundProperty,
                    Resource(foregroundKey)),
                new Setter(
                    Button.BorderThicknessProperty,
                    new Thickness(0)),
                new Setter(
                    Button.CornerRadiusProperty,
                    new CornerRadius(GraphicalProfileColors.PrimaryRadius)),
            },
        };

    private static DynamicResourceExtension Resource(string key) =>
        new(key);
}
