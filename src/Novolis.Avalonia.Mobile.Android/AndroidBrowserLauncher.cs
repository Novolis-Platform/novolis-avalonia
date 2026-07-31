using Android.Content;
using AndroidX.Browser.CustomTabs;
using Novolis.Avalonia.Mobile;

namespace Novolis.Avalonia.Mobile.Android;

/// <summary>Opens HTTPS URLs in Chrome Custom Tabs (falls back to ACTION_VIEW).</summary>
public sealed class AndroidBrowserLauncher : IBrowserLauncher
{
    readonly Context _context;

    /// <summary>Uses <see cref="Application.Context"/>.</summary>
    public AndroidBrowserLauncher()
        : this(global::Android.App.Application.Context
               ?? throw new InvalidOperationException("Android Application.Context is not available."))
    {
    }

    /// <summary>Uses the given context.</summary>
    public AndroidBrowserLauncher(Context context) =>
        _context = context.ApplicationContext ?? context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc />
    public Task OpenAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);
        cancellationToken.ThrowIfCancellationRequested();

        var androidUri = global::Android.Net.Uri.Parse(uri.AbsoluteUri)
            ?? throw new InvalidOperationException($"Could not parse URI '{uri}'.");

        try
        {
            var builder = new CustomTabsIntent.Builder();
            var customTabs = builder.Build();
            customTabs.Intent.AddFlags(ActivityFlags.NewTask);
            customTabs.LaunchUrl(_context, androidUri);
        }
        catch
        {
            var intent = new Intent(Intent.ActionView);
            intent.SetData(androidUri);
            intent.AddFlags(ActivityFlags.NewTask);
            _context.StartActivity(intent);
        }

        return Task.CompletedTask;
    }
}
