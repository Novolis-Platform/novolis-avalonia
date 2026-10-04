namespace Novolis.Avalonia.Mobile;

/// <summary>Keeps the device screen awake while the host is presenting media.</summary>
public interface IScreenWakeLock
{
    /// <summary>Acquire a wake lock; dispose to release.</summary>
    IDisposable Acquire(string reason);
}
