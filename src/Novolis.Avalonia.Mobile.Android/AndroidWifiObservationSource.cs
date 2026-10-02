using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Novolis.Avalonia.Mobile;

namespace Novolis.Avalonia.Mobile.Android;

/// <summary>Reads the connected network and the names already heard nearby.</summary>
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
                        : MobileObservationStatus.Available,
                    ReadVisible(ssid)));
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

    /// <inheritdoc />
    public async ValueTask<MobileWifiReading> RefreshVisibleAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (GetStatus().IsAvailable)
            {
#pragma warning disable CA1422 // StartScan remains the way to refresh a place picker.
                _wifiManager.StartScan();
#pragma warning restore CA1422
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
        catch (global::Java.Lang.SecurityException)
        {
            // A cached scan is still useful when a fresh one is denied.
        }

        return await ReadAsync(cancellationToken);
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

    IReadOnlyList<MobileWifiNetwork> ReadVisible(string? connectedSsid)
    {
        try
        {
            var results = _wifiManager.ScanResults;
            if (results is null || results.Count == 0)
                return [];

            var elapsedUs = SystemClock.ElapsedRealtime() * 1000L;
            var heard = new Dictionary<string, MobileWifiNetwork>(StringComparer.Ordinal);
            foreach (var result in results)
            {
                var ssid = ReadResultSsid(result);
                if (ssid is null)
                    continue;

                TimeSpan? age = null;
                if (result.Timestamp > 0)
                {
                    var ageUs = elapsedUs - result.Timestamp;
                    age = ageUs <= 0
                        ? TimeSpan.Zero
                        : TimeSpan.FromMicroseconds(ageUs);
                }

                var network = new MobileWifiNetwork(
                    ssid,
                    WifiPlacementConnected(ssid, connectedSsid),
                    result.Level,
                    age);
                if (!heard.TryGetValue(ssid, out var existing)
                    || (network.SignalDbm ?? int.MinValue) > (existing.SignalDbm ?? int.MinValue))
                    heard[ssid] = network;
            }

            return heard.Values
                .OrderByDescending(item => item.IsConnected)
                .ThenByDescending(item => item.SignalDbm ?? int.MinValue)
                .ToArray();
        }
        catch (global::Java.Lang.SecurityException)
        {
            return [];
        }
    }

    static bool WifiPlacementConnected(string ssid, string? connectedSsid) =>
        !string.IsNullOrWhiteSpace(connectedSsid)
        && string.Equals(ssid, connectedSsid, StringComparison.Ordinal);

    static string? ReadResultSsid(ScanResult result)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
            return NormalizeSsid(result.WifiSsid?.ToString());

#pragma warning disable CA1422
        return NormalizeSsid(result.Ssid);
#pragma warning restore CA1422
    }

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
