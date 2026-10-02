namespace Novolis.Avalonia.Mobile;

/// <summary>One network name heard or joined, without the rest of a scan.</summary>
public sealed record MobileWifiNetwork(
    string Ssid,
    bool IsConnected,
    int? SignalDbm,
    TimeSpan? Age);
