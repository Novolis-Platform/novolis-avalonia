using System.Text;
using System.Text.Json;
using Avalonia.Controls;
using Novolis.Avalonia._3D.Session;
using Novolis.Avalonia._3D.Ui;
using Novolis.Math.Geometry;
using Novolis.Modeling.Scene;

namespace Novolis.Avalonia._3D.Services;

/// <summary>Writes inspectable SceneLab artifacts (viewport PNG, window PNG, scene JSON, mesh OBJ/stats, manifest).</summary>
public sealed class SceneArtifactDumper
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public SceneArtifactDumper(SceneSessionService session, string dataRoot)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
        DataRoot = string.IsNullOrWhiteSpace(dataRoot)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Novolis", "SceneLab")
            : dataRoot;
    }

    public SceneSessionService Session { get; }
    public string DataRoot { get; }
    public string DumpsDirectory => SceneViewportExporter.DumpsDirectory(DataRoot);
    public string ManifestPath => Path.Combine(DumpsDirectory, "last-artifact.json");

    public async Task<SceneArtifactResult> DumpAsync(
        string kind,
        Window? window,
        SceneViewportControl? viewport,
        CancellationToken cancellationToken = default)
    {
        kind = (kind ?? "all").Trim().ToLowerInvariant();
        Directory.CreateDirectory(DumpsDirectory);

        var result = new SceneArtifactResult
        {
            Kind = kind,
            CapturedAtUtc = DateTime.UtcNow.ToString("O"),
            DocumentPath = Session.DocumentPath,
            DocumentName = Session.Document.Name,
            NodeCount = Session.Document.Nodes.Count,
            ManifestPath = ManifestPath,
        };

        if (kind is "all" or "scene" or "dumpscene")
            result.ScenePath = DumpSceneCopy();

        if (kind is "all" or "mesh" or "dumpmesh")
        {
            var (obj, stats) = DumpMeshArtifacts();
            result.MeshObjPath = obj;
            result.MeshStatsPath = stats;
            FillMeshStats(result);
        }

        if (kind is "all" or "viewport" or "dumpviewport")
        {
            if (viewport is null)
                result.Notes = Append(result.Notes, "viewport control missing");
            else
            {
                var path = SceneViewportExporter.AllocatePath(DumpsDirectory, "viewport");
                var ok = await SceneViewportExporter.ExportViewportPngAsync(viewport, path, cancellationToken)
                    .ConfigureAwait(true);
                result.ViewportPngPath = ok ? path : null;
                if (!ok)
                    result.Notes = Append(result.Notes, "viewport png failed");
            }
        }

        if (kind is "all" or "window" or "dumpui" or "dumpwindow")
        {
            if (window is null)
                result.Notes = Append(result.Notes, "window missing");
            else
            {
                var path = SceneViewportExporter.AllocatePath(DumpsDirectory, "window");
                var ok = SceneViewportExporter.TryExportControlPng(window, path);
                result.WindowPngPath = ok ? path : null;
                if (!ok)
                    result.Notes = Append(result.Notes, "window png failed");
            }
        }

        if (viewport is not null)
        {
            result.Backend = viewport.Backend.ToString();
            result.LastPresentMs = viewport.FrameMeter.LastMs;
            result.AvgPresentMs = viewport.FrameMeter.AvgMs;
            result.FpsEstimate = viewport.FrameMeter.Fps;
            result.ViewportError = viewport.LastError;
        }

        await File.WriteAllTextAsync(ManifestPath, JsonSerializer.Serialize(result, Json), cancellationToken)
            .ConfigureAwait(false);
        await File.WriteAllTextAsync(
                Path.Combine(DumpsDirectory, "last-document.path"),
                (Session.DocumentPath ?? "") + Environment.NewLine,
                cancellationToken)
            .ConfigureAwait(false);

        return result;
    }

    private string DumpSceneCopy()
    {
        var path = SceneViewportExporter.AllocatePath(DumpsDirectory, "scene", "nov3djson");
        if (!string.IsNullOrWhiteSpace(Session.DocumentPath) && File.Exists(Session.DocumentPath))
            File.Copy(Session.DocumentPath, path, overwrite: true);
        else
            SceneSerializer.Save(Session.Document, path);
        return path;
    }

    private (string? ObjPath, string StatsPath) DumpMeshArtifacts()
    {
        var statsPath = SceneViewportExporter.AllocatePath(DumpsDirectory, "mesh-stats", "json");
        var mesh = FindPrimaryMesh(Session.Document);
        if (mesh is null)
        {
            File.WriteAllText(statsPath, JsonSerializer.Serialize(new { error = "no mesh node" }, Json));
            return (null, statsPath);
        }

        var editable = MeshEditBake.ReadBakedOrTessellate(mesh);
        var objPath = SceneViewportExporter.AllocatePath(DumpsDirectory, "mesh", "obj");
        WriteWavefrontObj(editable, mesh.Name, objPath);

        var stats = new
        {
            meshId = mesh.Id,
            name = mesh.Name,
            vertexCount = editable.VertexCount,
            triangleCount = editable.TriangleCount,
            indexCount = editable.Indices.Count,
            baked = mesh.Vertices is { Length: > 0 },
        };
        File.WriteAllText(statsPath, JsonSerializer.Serialize(stats, Json));
        return (objPath, statsPath);
    }

    private void FillMeshStats(SceneArtifactResult result)
    {
        var mesh = FindPrimaryMesh(Session.Document);
        if (mesh is null)
            return;
        var editable = MeshEditBake.ReadBakedOrTessellate(mesh);
        result.VertexCount = editable.VertexCount;
        result.TriangleCount = editable.TriangleCount;
        result.MeshName = mesh.Name;
    }

    private static MeshNode? FindPrimaryMesh(SceneDocument document)
    {
        if (document.SelectionId is { } sel && document.Find(sel) is MeshNode selected)
            return selected;
        return document.Nodes.OfType<MeshNode>().FirstOrDefault();
    }

    private static void WriteWavefrontObj(EditableMesh mesh, string name, string path)
    {
        var sb = new StringBuilder(mesh.VertexCount * 32 + mesh.TriangleCount * 24);
        sb.Append("# Novolis SceneLab mesh dump").AppendLine();
        sb.Append("o ").Append(Sanitize(name)).AppendLine();
        foreach (var v in mesh.Vertices)
            sb.Append("v ").Append(v.X.ToString("G9")).Append(' ')
                .Append(v.Y.ToString("G9")).Append(' ')
                .Append(v.Z.ToString("G9")).AppendLine();
        var idx = mesh.Indices;
        for (var i = 0; i + 2 < idx.Count; i += 3)
        {
            // OBJ is 1-based
            sb.Append("f ").Append(idx[i] + 1).Append(' ')
                .Append(idx[i + 1] + 1).Append(' ')
                .Append(idx[i + 2] + 1).AppendLine();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, sb.ToString());
    }

    private static string Sanitize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "mesh";
        var chars = name.Select(c => char.IsLetterOrDigit(c) || c is '_' or '-' ? c : '_').ToArray();
        return new string(chars);
    }

    private static string Append(string? existing, string note) =>
        string.IsNullOrWhiteSpace(existing) ? note : existing + "; " + note;
}

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
