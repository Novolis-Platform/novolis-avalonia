using Avalonia.Media.Imaging;
using Novolis.IO.Maps;

namespace Novolis.Avalonia.Map;

/// <summary>A decoded raster tile supplied by an application-level provider.</summary>
public sealed record MapTile(MapTileKey Key, Bitmap Image, bool IsStale = false);
