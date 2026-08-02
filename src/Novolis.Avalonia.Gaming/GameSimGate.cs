namespace Novolis.Avalonia.Gaming;

/// <summary>Pure gate for whether a frame should advance game simulation.</summary>
public static class GameSimGate
{
    /// <summary>
    /// Returns whether simulation should tick this frame.
    /// </summary>
    /// <param name="pauseMode">HardPause freezes on pause/modal; RunAlways never freezes for UI.</param>
    /// <param name="isPaused">Explicit pause (Escape menu, pause button).</param>
    /// <param name="modalOpen">True while a blocking modal/menu overlay is shown.</param>
    public static bool ShouldTick(GamePauseMode pauseMode, bool isPaused, bool modalOpen)
    {
        if (pauseMode == GamePauseMode.RunAlways)
            return true;

        return !isPaused && !modalOpen;
    }
}
