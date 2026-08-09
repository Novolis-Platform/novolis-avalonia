using System.Drawing;
using System.Numerics;
using Novolis._3D;

namespace Novolis.Avalonia._3D.Services;

/// <summary>
/// Cached unique-edge wireframe submitted as one rlgl LINES batch (not per-edge DrawLine3D).
/// </summary>
internal sealed class WireMeshCache
{
    private Vector3[]? _verticesRef;
    private int[]? _indicesRef;
    private int[] _edgeA = [];
    private int[] _edgeB = [];
    private Vector3[] _world = [];

    public void Draw(EvaluatedMesh mesh, Color color, HashSet<(int A, int B)>? highlightEdges, Color highlight)
    {
        EnsureTopology(mesh);
        EnsureWorld(mesh);

        RlglLines.Begin(RlglLines.Lines);
        RlglLines.Color(color);
        for (var i = 0; i < _edgeA.Length; i++)
        {
            var a = _edgeA[i];
            var b = _edgeB[i];
            if (highlightEdges is not null)
            {
                var key = a < b ? (a, b) : (b, a);
                if (highlightEdges.Contains(key))
                    continue;
            }

            RlglLines.Vertex(_world[a]);
            RlglLines.Vertex(_world[b]);
        }

        if (highlightEdges is { Count: > 0 })
        {
            RlglLines.Color(highlight);
            foreach (var (a, b) in highlightEdges)
            {
                if ((uint)a >= (uint)_world.Length || (uint)b >= (uint)_world.Length)
                    continue;
                RlglLines.Vertex(_world[a]);
                RlglLines.Vertex(_world[b]);
            }
        }

        RlglLines.End();
    }

    public void DrawFaceStars(EvaluatedMesh mesh, IEnumerable<int> faces, Color color)
    {
        EnsureTopology(mesh);
        EnsureWorld(mesh);
        RlglLines.Begin(RlglLines.Lines);
        RlglLines.Color(color);
        foreach (var face in faces)
        {
            var t = face * 3;
            if (t + 2 >= mesh.Indices.Length)
                continue;
            var i0 = mesh.Indices[t];
            var i1 = mesh.Indices[t + 1];
            var i2 = mesh.Indices[t + 2];
            var mid = (_world[i0] + _world[i1] + _world[i2]) / 3f;
            RlglLines.Vertex(_world[i0]);
            RlglLines.Vertex(mid);
            RlglLines.Vertex(_world[i1]);
            RlglLines.Vertex(mid);
            RlglLines.Vertex(_world[i2]);
            RlglLines.Vertex(mid);
        }

        RlglLines.End();
    }

    public void DrawPoints(EvaluatedMesh mesh, float size, Color color, IReadOnlySet<int>? selected, Color selectedColor)
    {
        EnsureWorld(mesh);
        var half = size * 0.5f;
        RlglLines.Begin(RlglLines.Lines);
        for (var i = 0; i < _world.Length; i++)
        {
            var p = _world[i];
            RlglLines.Color(selected is not null && selected.Contains(i) ? selectedColor : color);
            // Axis-aligned cross — cheap screen-readable point without sphere tessellation.
            RlglLines.Vertex(p + new Vector3(-half, 0, 0));
            RlglLines.Vertex(p + new Vector3(half, 0, 0));
            RlglLines.Vertex(p + new Vector3(0, -half, 0));
            RlglLines.Vertex(p + new Vector3(0, half, 0));
        }

        RlglLines.End();
    }

    private void EnsureTopology(EvaluatedMesh mesh)
    {
        if (ReferenceEquals(_verticesRef, mesh.Vertices)
            && ReferenceEquals(_indicesRef, mesh.Indices)
            && _edgeA.Length > 0)
            return;

        _verticesRef = mesh.Vertices;
        _indicesRef = mesh.Indices;

        var set = new HashSet<(int, int)>();
        var indices = mesh.Indices;
        for (var t = 0; t + 2 < indices.Length; t += 3)
        {
            Add(set, indices[t], indices[t + 1]);
            Add(set, indices[t + 1], indices[t + 2]);
            Add(set, indices[t + 2], indices[t]);
        }

        _edgeA = new int[set.Count];
        _edgeB = new int[set.Count];
        var n = 0;
        foreach (var (a, b) in set)
        {
            _edgeA[n] = a;
            _edgeB[n] = b;
            n++;
        }

        _world = new Vector3[mesh.Vertices.Length];
    }

    private void EnsureWorld(EvaluatedMesh mesh)
    {
        if (_world.Length != mesh.Vertices.Length)
            _world = new Vector3[mesh.Vertices.Length];
        var m = mesh.World;
        var src = mesh.Vertices;
        for (var i = 0; i < src.Length; i++)
            _world[i] = Vector3.Transform(src[i], m);
    }

    private static void Add(HashSet<(int, int)> set, int a, int b)
    {
        if (a == b)
            return;
        set.Add(a < b ? (a, b) : (b, a));
    }
}
