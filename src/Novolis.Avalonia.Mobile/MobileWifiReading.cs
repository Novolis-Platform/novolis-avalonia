using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Mobile;

/// <summary>A platform Wi-Fi sample without application-specific meaning.</summary>
public sealed record MobileWifiReading(
    DateTimeOffset At,
    string? ConnectedSsid,
    MobileObservationStatus Status);
