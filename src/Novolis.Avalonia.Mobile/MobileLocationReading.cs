using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Mobile;

/// <summary>A platform location sample without application-specific meaning.</summary>
public sealed record MobileLocationReading(
    DateTimeOffset At,
    GeoCoordinate Position,
    double AccuracyMeters);
