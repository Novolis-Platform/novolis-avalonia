using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Mobile;

/// <summary>Reads the currently connected Wi-Fi network when platform policy permits.</summary>
public interface IWifiObservationSource
{
    /// <summary>Returns the current platform capability state.</summary>
    MobileSourceStatus GetStatus();

    /// <summary>Reads one current Wi-Fi sample.</summary>
    ValueTask<MobileWifiReading> ReadAsync(
        CancellationToken cancellationToken = default);
}
