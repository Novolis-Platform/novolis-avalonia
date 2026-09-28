using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Mobile;

/// <summary>Availability state reported by a mobile observation source.</summary>
public enum MobileObservationStatus
{
    /// <summary>The platform can currently provide this observation.</summary>
    Available,

    /// <summary>A required runtime permission is not granted.</summary>
    PermissionDenied,

    /// <summary>The device location service is disabled.</summary>
    LocationServicesDisabled,

    /// <summary>The platform returned a redacted value.</summary>
    Redacted,

    /// <summary>The platform cannot provide the observation.</summary>
    Unavailable,
}

/// <summary>Availability and optional diagnostic detail for a mobile source.</summary>
public readonly record struct MobileSourceStatus(
    MobileObservationStatus Status,
    string? Detail = null)
{
    /// <summary>Whether the source can currently provide useful readings.</summary>
    public bool IsAvailable => Status == MobileObservationStatus.Available;
}

/// <summary>A platform location sample without application-specific meaning.</summary>
public sealed record MobileLocationReading(
    DateTimeOffset At,
    GeoCoordinate Position,
    double AccuracyMeters);

/// <summary>A platform Wi-Fi sample without application-specific meaning.</summary>
public sealed record MobileWifiReading(
    DateTimeOffset At,
    string? ConnectedSsid,
    MobileObservationStatus Status);

/// <summary>Produces sparse location readings from a mobile platform.</summary>
public interface ILocationReadingSource
{
    /// <summary>Returns the current platform capability state.</summary>
    MobileSourceStatus GetStatus();

    /// <summary>Observes readings until cancellation or source completion.</summary>
    IAsyncEnumerable<MobileLocationReading> ObserveAsync(
        TimeSpan minimumInterval,
        double minimumDistanceMeters,
        CancellationToken cancellationToken = default);
}

/// <summary>Reads the currently connected Wi-Fi network when platform policy permits.</summary>
public interface IWifiObservationSource
{
    /// <summary>Returns the current platform capability state.</summary>
    MobileSourceStatus GetStatus();

    /// <summary>Reads one current Wi-Fi sample.</summary>
    ValueTask<MobileWifiReading> ReadAsync(
        CancellationToken cancellationToken = default);
}
