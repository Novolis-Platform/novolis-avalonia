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
