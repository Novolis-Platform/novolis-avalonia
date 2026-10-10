using System.Text;
using System.Text.Json;
using Avalonia.Controls;
using Novolis.Avalonia.Modeling.Session;
using Novolis.Avalonia.Modeling.Ui;
using Novolis.Math.Geometry;
using Novolis.Modeling;

namespace Novolis.Avalonia.Modeling.Services;

public sealed class SceneArtifactResult
{
    public string Kind { get; init; } = "all";
    public string? DocumentPath { get; init; }
    public string? DocumentName { get; init; }
    public int NodeCount { get; init; }
    public string? ScenePath { get; set; }
    public string? ViewportPngPath { get; set; }
    public string? WindowPngPath { get; set; }
    public string? MeshObjPath { get; set; }
    public string? MeshStatsPath { get; set; }
    public string? MeshName { get; set; }
    public int VertexCount { get; set; }
    public int TriangleCount { get; set; }
    public string ManifestPath { get; init; } = "";
    public string CapturedAtUtc { get; init; } = "";
    public string? Backend { get; set; }
    public double LastPresentMs { get; set; }
    public double AvgPresentMs { get; set; }
    public double FpsEstimate { get; set; }
    public string? ViewportError { get; set; }
    public string? Notes { get; set; }
}
