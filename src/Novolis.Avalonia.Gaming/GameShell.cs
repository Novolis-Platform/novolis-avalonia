using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Novolis.Avalonia.Rendering;

namespace Novolis.Avalonia.Gaming;

/// <summary>
/// Game root: Silk/TwoD (or any) viewport under Avalonia HUD + modal menu layers.
/// Use this when interactive menus must sit above <see cref="TwoDSceneControl"/> without drawing UI into GL.
/// </summary>
public sealed class GameShell : Grid
{
    readonly ContentControl _viewportHost = new()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch,
    };
    readonly ContentControl _hudHost = new()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch,
        IsHitTestVisible = true,
    };
    readonly ModalMenuHost _modal = new();

    /// <summary>Viewport control (typically <see cref="TwoDSceneControl"/>).</summary>
    public static readonly StyledProperty<Control?> ViewportProperty =
        AvaloniaProperty.Register<GameShell, Control?>(nameof(Viewport));

    /// <summary>Non-modal HUD content (strips, side rails). Drawn above the viewport.</summary>
    public static readonly StyledProperty<Control?> HudProperty =
        AvaloniaProperty.Register<GameShell, Control?>(nameof(Hud));

    /// <summary>How sim time reacts to pause / modal (default <see cref="GamePauseMode.HardPause"/>).</summary>
    public static readonly StyledProperty<GamePauseMode> PauseModeProperty =
        AvaloniaProperty.Register<GameShell, GamePauseMode>(nameof(PauseMode), GamePauseMode.HardPause);

    /// <summary>Explicit pause flag (Escape menu, pause button).</summary>
    public static readonly StyledProperty<bool> IsPausedProperty =
        AvaloniaProperty.Register<GameShell, bool>(nameof(IsPaused));

    /// <summary>Creates an empty shell with HardPause defaults.</summary>
    public GameShell()
    {
        RowDefinitions = new RowDefinitions("*,Auto");

        // Viewport only in the stretch row so HUD chrome does not steal map clicks.
        Grid.SetRow(_viewportHost, 0);
        Children.Add(_viewportHost);

        Grid.SetRow(_hudHost, 1);
        Children.Add(_hudHost);

        Grid.SetRow(_modal, 0);
        Grid.SetRowSpan(_modal, 2);
        Children.Add(_modal);

        _modal.DismissRequested += (_, _) => DismissModal();

        ViewportProperty.Changed.AddClassHandler<GameShell>((s, _) => s._viewportHost.Content = s.Viewport);
        HudProperty.Changed.AddClassHandler<GameShell>((s, _) => s._hudHost.Content = s.Hud);
    }

    /// <summary>Viewport under the HUD/modal layers.</summary>
    public Control? Viewport
    {
        get => GetValue(ViewportProperty);
        set => SetValue(ViewportProperty, value);
    }

    /// <summary>HUD overlay (usually not full-bleed; docked via its own alignment).</summary>
    public Control? Hud
    {
        get => GetValue(HudProperty);
        set => SetValue(HudProperty, value);
    }

    /// <summary>Pause policy for interactive vs headless hosts.</summary>
    public GamePauseMode PauseMode
    {
        get => GetValue(PauseModeProperty);
        set => SetValue(PauseModeProperty, value);
    }

    /// <summary>Explicit pause.</summary>
    public bool IsPaused
    {
        get => GetValue(IsPausedProperty);
        set => SetValue(IsPausedProperty, value);
    }

    /// <summary>True while a modal menu is visible.</summary>
    public bool IsModalOpen => _modal.IsVisible;

    /// <summary>The modal host (for advanced composition).</summary>
    public ModalMenuHost Modal => _modal;

    /// <summary>Convenience cast when <see cref="Viewport"/> is a <see cref="TwoDSceneControl"/>.</summary>
    public TwoDSceneControl? TwoDViewport => Viewport as TwoDSceneControl;

    /// <summary>Whether simulation should advance this frame under current pause/modal state.</summary>
    public bool ShouldAdvanceSimulation() =>
        GameSimGate.ShouldTick(PauseMode, IsPaused, IsModalOpen);

    /// <summary>Shows a blocking menu; under HardPause this freezes sim ticks until dismissed.</summary>
    public void ShowModal(Control content) => _modal.Show(content);

    /// <summary>Hides the modal menu.</summary>
    public void DismissModal() => _modal.Dismiss();

    /// <summary>
    /// Builds a shell with a <see cref="TwoDSceneControl"/> viewport and optional HUD.
    /// </summary>
    public static GameShell CreateWithTwoD(Control? hud = null, GamePauseMode pauseMode = GamePauseMode.HardPause)
    {
        var twoD = new TwoDSceneControl
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        return new GameShell
        {
            Viewport = twoD,
            Hud = hud,
            PauseMode = pauseMode,
        };
    }
}
