using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Novolis.Avalonia.Audio;

/// <summary>Magix-style edit tool modes for the arrangement timeline.</summary>
public enum ArrangementTool
{
    /// <summary>Select clips, scrub, time-range, trim handles.</summary>
    Select,

    /// <summary>Drag clips in time and across tracks.</summary>
    Move,

    /// <summary>Click a clip to split at the pointer (razor).</summary>
    Split,

    /// <summary>Click empty lane to place the selected library sound at playhead/time.</summary>
    Draw,

    /// <summary>Click a clip to delete it.</summary>
    Delete,
}
