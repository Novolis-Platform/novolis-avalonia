using Avalonia.Media.Imaging;
using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Map;

/// <summary>A decoded raster tile supplied by an application-level provider.</summary>
public sealed record MapTile(MapTileKey Key, Bitmap Image);
