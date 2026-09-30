using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Mobile;

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
