using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Mobile;

/// <summary>Availability and optional diagnostic detail for a mobile source.</summary>
public readonly record struct MobileSourceStatus(
    MobileObservationStatus Status,
    string? Detail = null)
{
    /// <summary>Whether the source can currently provide useful readings.</summary>
    public bool IsAvailable => Status == MobileObservationStatus.Available;
}
