namespace Novolis.Avalonia.Mobile;

/// <summary>No-op wake lock for hosts without a screen policy.</summary>
public sealed class NullScreenWakeLock : IScreenWakeLock
{
    /// <inheritdoc />
    public IDisposable Acquire(string reason) => Empty.Instance;

    private sealed class Empty : IDisposable
    {
        public static readonly Empty Instance = new();

        public void Dispose()
        {
        }
    }
}
