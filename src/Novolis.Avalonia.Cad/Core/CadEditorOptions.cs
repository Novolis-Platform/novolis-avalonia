using System.Text.Json;

namespace Novolis.Avalonia.Cad.Core;

/// <summary>Mutable editor preferences (snap, grid, elevation, last document).</summary>
public sealed class CadEditorOptions
{
    public double LeftColumnPixels { get; set; } = 260;

    public double RightColumnPixels { get; set; } = 280;

    public bool SnapToGrid { get; set; } = true;

    public float GridStep { get; set; } = 0.5f;

    public string ViewMode { get; set; } = "draft";

    public string DisplayUnit { get; set; } = CadUnits.Meter;

    public string? LastDocumentPath { get; set; }

    public float DrawElevation { get; set; }

    public bool ContinuousLine { get; set; }

    public bool IsolateLevel { get; set; } = true;

    public float LevelTolerance { get; set; } = 0.05f;

    /// <summary>Constrain interactive move: <c>none</c>, <c>x</c>, <c>y</c>, or <c>z</c>.</summary>
    public string AxisLock { get; set; } = "none";
}
