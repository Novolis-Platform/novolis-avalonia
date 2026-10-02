using Avalonia.Media;

namespace Novolis.Avalonia.Map;

/// <summary>Mixes map ink colors. Entity color stays on the map, not in chrome.</summary>
public static class MapInk
{
    /// <summary>Linear blend from <paramref name="from"/> toward <paramref name="to"/>.</summary>
    public static Color Lerp(Color from, Color to, double amount)
    {
        var t = double.IsFinite(amount) ? global::System.Math.Clamp(amount, 0, 1) : 0;
        return Color.FromArgb(
            Channel(from.A, to.A, t),
            Channel(from.R, to.R, t),
            Channel(from.G, to.G, t),
            Channel(from.B, to.B, t));
    }

    static byte Channel(byte from, byte to, double amount) =>
        (byte)global::System.Math.Round(from + ((to - from) * amount));
}
