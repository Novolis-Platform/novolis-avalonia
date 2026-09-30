using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Novolis.Audio.Live.Visuals;

namespace Novolis.Avalonia.Live;

/// <summary>Contract for a Live visualizer panel.</summary>
public interface ILiveVisualizer
{
    string Title { get; }

    Control View { get; }

    void Bind(LiveVisualizerModel model);
}
