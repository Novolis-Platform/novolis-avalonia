using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.OS;
using Novolis.Avalonia.Mobile;

namespace Novolis.Avalonia.Mobile.Android;

/// <summary>Streams sparse Android location readings without interpreting them.</summary>
public sealed class AndroidLocationReadingSource : ILocationReadingSource
{
    readonly Context _context;
    readonly LocationManager _locationManager;

    /// <summary>Creates a source using the current Android application context.</summary>
    public AndroidLocationReadingSource()
        : this(
            global::Android.App.Application.Context
            ?? throw new InvalidOperationException("Android Application.Context is not available."))
    {
    }

    /// <summary>Creates a source for the supplied Android context.</summary>
    public AndroidLocationReadingSource(Context context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context.ApplicationContext ?? context;
        _locationManager = _context.GetSystemService(Context.LocationService) as LocationManager
            ?? throw new InvalidOperationException("Android LocationManager is unavailable.");
    }

    /// <inheritdoc />
    public MobileSourceStatus GetStatus()
    {
        if (!HasLocationPermission())
            return new MobileSourceStatus(
                MobileObservationStatus.PermissionDenied,
                "Location permission is not granted.");

        try
        {
            var hasProvider = _locationManager.IsProviderEnabled(LocationManager.GpsProvider)
                || _locationManager.IsProviderEnabled(LocationManager.NetworkProvider);
            return hasProvider
                ? new MobileSourceStatus(MobileObservationStatus.Available)
                : new MobileSourceStatus(
                    MobileObservationStatus.LocationServicesDisabled,
                    "No Android location provider is enabled.");
        }
        catch (global::Java.Lang.SecurityException)
        {
            return new MobileSourceStatus(
                MobileObservationStatus.PermissionDenied,
                "Android denied access to location providers.");
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<MobileLocationReading> ObserveAsync(
        TimeSpan minimumInterval,
        double minimumDistanceMeters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (minimumInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(minimumInterval));

        if (!double.IsFinite(minimumDistanceMeters) || minimumDistanceMeters < 0)
            throw new ArgumentOutOfRangeException(nameof(minimumDistanceMeters));

        if (!GetStatus().IsAvailable)
            yield break;

        var channel = Channel.CreateUnbounded<MobileLocationReading>();
        var listener = new ChannelLocationListener(channel.Writer);
        var provider = GetProvider();

        try
        {
            _locationManager.RequestLocationUpdates(
                provider,
                (long)global::System.Math.Max(1, minimumInterval.TotalMilliseconds),
                (float)minimumDistanceMeters,
                listener,
                Looper.MainLooper);
        }
        catch (global::Java.Lang.SecurityException)
        {
            yield break;
        }

        try
        {
            await foreach (var reading in channel.Reader.ReadAllAsync(cancellationToken))
                yield return reading;
        }
        finally
        {
            try
            {
                _locationManager.RemoveUpdates(listener);
            }
            catch (global::Java.Lang.SecurityException)
            {
                // Permission may have been revoked while the source was active.
            }

            channel.Writer.TryComplete();
        }
    }

    /// <inheritdoc />
    public async ValueTask<MobileLocationReading?> ReadFixAsync(
        TimeSpan maximumAge,
        CancellationToken cancellationToken = default)
    {
        if (maximumAge < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(maximumAge));

        cancellationToken.ThrowIfCancellationRequested();
        if (!GetStatus().IsAvailable)
            return null;

        var known = ReadLastKnown(maximumAge);
        if (known is not null)
            return known;

        var channel = Channel.CreateBounded<MobileLocationReading>(1);
        var listener = new ChannelLocationListener(channel.Writer);
        try
        {
            _locationManager.RequestLocationUpdates(
                GetProvider(),
                0,
                0,
                listener,
                Looper.MainLooper);
        }
        catch (global::Java.Lang.SecurityException)
        {
            return null;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            return await channel.Reader.ReadAsync(timeout.Token);
        }
        catch (System.OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ReadLastKnown(TimeSpan.FromMinutes(15));
        }
        finally
        {
            try
            {
                _locationManager.RemoveUpdates(listener);
            }
            catch (global::Java.Lang.SecurityException)
            {
                // Permission may have been revoked while the fix was pending.
            }

            channel.Writer.TryComplete();
        }
    }

    MobileLocationReading? ReadLastKnown(TimeSpan maximumAge)
    {
        global::Android.Locations.Location? best = null;
        foreach (var provider in new[]
                 {
                     LocationManager.GpsProvider,
                     LocationManager.NetworkProvider,
                 })
        {
            try
            {
                if (!_locationManager.IsProviderEnabled(provider))
                    continue;

                var location = _locationManager.GetLastKnownLocation(provider);
                if (location is null)
                    continue;

                if (best is null || location.Time > best.Time)
                    best = location;
            }
            catch (global::Java.Lang.SecurityException)
            {
                // This provider is not readable. Try the other one.
            }
        }

        if (best is null || best.Time <= 0)
            return null;

        var at = DateTimeOffset.FromUnixTimeMilliseconds(best.Time);
        if (DateTimeOffset.UtcNow - at > maximumAge)
            return null;

        var accuracy = best.Accuracy > 0 ? best.Accuracy : 1_000;
        try
        {
            return new MobileLocationReading(
                at,
                new Novolis.Math.Geometry.GeoCoordinate(best.Latitude, best.Longitude),
                accuracy);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    bool HasLocationPermission() =>
        _context.CheckSelfPermission(global::Android.Manifest.Permission.AccessFineLocation)
            == Permission.Granted
        || _context.CheckSelfPermission(global::Android.Manifest.Permission.AccessCoarseLocation)
            == Permission.Granted;

    string GetProvider()
    {
        // A 50 m geofence needs the fine GPS provider when it is available.
        // Network fixes remain a useful fallback on devices that disable GPS.
        if (_locationManager.IsProviderEnabled(LocationManager.GpsProvider))
            return LocationManager.GpsProvider;

        if (_locationManager.IsProviderEnabled(LocationManager.NetworkProvider))
            return LocationManager.NetworkProvider;

        return LocationManager.GpsProvider;
    }

    sealed class ChannelLocationListener(ChannelWriter<MobileLocationReading> writer)
        : Java.Lang.Object, ILocationListener
    {
        public void OnLocationChanged(Location location)
        {
            if (location is null)
                return;

            try
            {
                var at = location.Time > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(location.Time)
                    : DateTimeOffset.UtcNow;
                var accuracy = location.Accuracy > 0 ? location.Accuracy : 1_000;
                writer.TryWrite(new MobileLocationReading(
                    at,
                    new Novolis.Math.Geometry.GeoCoordinate(location.Latitude, location.Longitude),
                    accuracy));
            }
            catch (ArgumentOutOfRangeException)
            {
                // Ignore malformed platform readings rather than emitting invalid coordinates.
            }
        }

        public void OnProviderDisabled(string provider)
        {
        }

        public void OnProviderEnabled(string provider)
        {
        }

        public void OnStatusChanged(string? provider, Availability status, Bundle? extras)
        {
        }
    }
}
