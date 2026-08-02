namespace Novolis.Avalonia.Gaming;

/// <summary>
/// How simulation time behaves when UI takes focus (menus, pause, modal dialogs).
/// </summary>
public enum GamePauseMode
{
    /// <summary>
    /// Interactive UIs: modal menus and explicit pause freeze sim ticks.
    /// Default for Avalonia / human play.
    /// </summary>
    HardPause = 0,

    /// <summary>
    /// Headless / autopilot: sim keeps advancing even while overlays are visible.
    /// </summary>
    RunAlways = 1,
}
