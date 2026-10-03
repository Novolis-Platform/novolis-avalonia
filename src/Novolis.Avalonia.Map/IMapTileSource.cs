using Avalonia.Media.Imaging;
using Novolis.IO.Maps;

namespace Novolis.Avalonia.Map;

/// <summary>Loads map tiles without coupling the control to a network provider.</summary>
public interface IMapTileSource
{
    /// <summary>Loads a tile, returning <see langword="null" /> when it is unavailable.</summary>
    ValueTask<MapTile?> GetTileAsync(
        MapTileKey key,
        CancellationToken cancellationToken = default);
}
