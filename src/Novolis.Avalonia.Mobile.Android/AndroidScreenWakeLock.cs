using Android.App;
using Android.Views;
using Novolis.Avalonia.Mobile;

namespace Novolis.Avalonia.Mobile.Android;

/// <summary>Keeps the current Android activity window awake via <see cref="WindowManagerFlags.KeepScreenOn"/>.</summary>
public sealed class AndroidScreenWakeLock : IScreenWakeLock
{
    private readonly Func<Activity?> currentActivity;

    /// <summary>Creates a wake lock that reads the current activity from <paramref name="currentActivity"/>.</summary>
    public AndroidScreenWakeLock(Func<Activity?> currentActivity)
    {
        this.currentActivity = currentActivity ?? throw new ArgumentNullException(nameof(currentActivity));
    }

    /// <inheritdoc />
    public IDisposable Acquire(string reason)
    {
        var activity = currentActivity();
        if (activity?.Window is null)
            return Empty.Instance;

        activity.RunOnUiThread(() =>
            activity.Window?.AddFlags(WindowManagerFlags.KeepScreenOn));
        return new Releaser(activity);
    }

    private sealed class Releaser : IDisposable
    {
        private readonly Activity activity;
        private int disposed;

        public Releaser(Activity activity) => this.activity = activity;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
                return;
            activity.RunOnUiThread(() =>
                activity.Window?.ClearFlags(WindowManagerFlags.KeepScreenOn));
        }
    }

    private sealed class Empty : IDisposable
    {
        public static readonly Empty Instance = new();

        public void Dispose()
        {
        }
    }
}
