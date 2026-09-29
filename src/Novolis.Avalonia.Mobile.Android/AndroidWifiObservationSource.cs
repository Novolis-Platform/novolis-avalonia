using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.Net.Wifi;
using Novolis.Avalonia.Mobile;

namespace Novolis.Avalonia.Mobile.Android;

/// <summary>Reads the currently connected Android Wi-Fi SSID without scanning nearby networks.</summary>
public sealed class AndroidWifiObservationSource : IWifiObservationSource
{
    readonly Context _context;
    readonly WifiManager _wifiManager;

    /// <summary>Creates a source using the current Android application context.</summary>
    public AndroidWifiObservationSource()
        : this(
            global::Android.App.Application.Context
            ?? throw new InvalidOperationException("Android Application.Context is not available."))
    {
    }

    /// <summary>Creates a source for the supplied Android context.</summary>
    public AndroidWifiObservationSource(Context context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context.ApplicationContext ?? context;
        _wifiManager = _context.GetSystemService(Context.WifiService) as WifiManager
            ?? throw new InvalidOperationException("Android WifiManager is unavailable.");
    }

    /// <inheritdoc />
    public MobileSourceStatus GetStatus()
    {
        if (!HasLocationPermission())
            return new MobileSourceStatus(
                MobileObservationStatus.PermissionDenied,
                "Location permission is required for SSID observation.");

        if (OperatingSystem.IsAndroidVersionAtLeast(33)
            && !HasPermission(global::Android.Manifest.Permission.NearbyWifiDevices))
            return new MobileSourceStatus(
                MobileObservationStatus.PermissionDenied,
                "Nearby Wi-Fi permission is not granted.");

        if (!HasPermission(global::Android.Manifest.Permission.AccessNetworkState)
            || !HasPermission(global::Android.Manifest.Permission.AccessWifiState))
            return new MobileSourceStatus(
                MobileObservationStatus.PermissionDenied,
                "Android Wi-Fi state permissions are not declared or granted.");

        try
        {
            return _wifiManager.IsWifiEnabled
                ? new MobileSourceStatus(MobileObservationStatus.Available)
                : new MobileSourceStatus(
                    MobileObservationStatus.Unavailable,
                    "Wi-Fi is disabled.");
        }
        catch (global::Java.Lang.SecurityException)
        {
            return new MobileSourceStatus(
                MobileObservationStatus.PermissionDenied,
                "Android denied access to Wi-Fi connection information.");
        }
    }

    /// <inheritdoc />
    public ValueTask<MobileWifiReading> ReadAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var at = DateTimeOffset.UtcNow;
        var status = GetStatus();
        if (!status.IsAvailable)
            return ValueTask.FromResult(new MobileWifiReading(at, null, status.Status));

        try
        {
            var ssid = ReadSsid();
            return ValueTask.FromResult(
                new MobileWifiReading(
                    at,
                    ssid,
                    ssid is null
                        ? MobileObservationStatus.Redacted
                        : MobileObservationStatus.Available));
        }
        catch (global::Java.Lang.SecurityException)
        {
            return ValueTask.FromResult(
                new MobileWifiReading(
                    at,
                    null,
                    MobileObservationStatus.PermissionDenied));
        }
    }

    string? ReadSsid()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            var connectivity = _context.GetSystemService(Context.ConnectivityService) as ConnectivityManager;
            var capabilities = connectivity?.GetNetworkCapabilities(connectivity.ActiveNetwork);
            if (capabilities?.TransportInfo is WifiInfo wifiInfo)
            {
                var ssid = NormalizeSsid(wifiInfo.SSID);
                if (ssid is not null)
                    return ssid;
            }

            // Some Android builds expose the active transport without its
            // WifiInfo wrapper. ConnectionInfo still provides the connected
            // SSID when the app has the location/nearby-device permissions.
            return ReadLegacySsid();
        }

        return ReadLegacySsid();
    }

#pragma warning disable CA1422 // ConnectionInfo remains the compatibility fallback on Android 31+.
    string? ReadLegacySsid() => NormalizeSsid(_wifiManager.ConnectionInfo?.SSID);
#pragma warning restore CA1422

    bool HasLocationPermission() =>
        HasPermission(global::Android.Manifest.Permission.AccessFineLocation)
        || HasPermission(global::Android.Manifest.Permission.AccessCoarseLocation);

    bool HasPermission(string permission) =>
        _context.CheckSelfPermission(permission) == Permission.Granted;

    static string? NormalizeSsid(string? ssid)
    {
        if (string.IsNullOrWhiteSpace(ssid))
            return null;

        var normalized = ssid.Trim();
        if (normalized.Length >= 2
            && normalized[0] == '"'
            && normalized[^1] == '"')
            normalized = normalized[1..^1];

        return normalized.Equals("<unknown ssid>", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("unknown ssid", StringComparison.OrdinalIgnoreCase)
            ? null
            : normalized;
    }
}
