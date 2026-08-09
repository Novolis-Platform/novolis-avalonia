using System.Drawing;
using System.Numerics;
using System.Text.Json;
using Novolis.Avalonia.Cad.Evaluation;
using Novolis.Cad.Primitives;
using Novolis.Raylib.Rendering;

namespace Novolis.Avalonia.Cad.Ship.Services;

/// <summary>
/// Ship Model-view exterior: draws authored solids/meshes from the <c>.cadjson</c> document.
/// No hardcoded freighter silhouette — geometry must be drawn in Draft Studio / Cad Studio and saved.
/// </summary>
public static class CadShipExterior
{
    private static readonly Color FallbackSolid = Color.FromArgb(255, 120, 150, 170);
    private static readonly Color AccentCargo = Color.FromArgb(255, 168, 128, 62);

    /// <summary>
    /// Use sealed exterior pass when the document looks like a ship and contains drawable solids/meshes.
    /// Isolate Level in the host still falls back to deck CAD (walls/spaces).
    /// </summary>
    public static bool ShouldUseExterior(CadDocument document) =>
        CadVec.LooksLikeShipDocument(document) && document.Entities.Any(IsExteriorDrawable);

    public static void Draw(CadDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        foreach (var entity in document.Entities)
            DrawOne(entity);
    }

    /// <summary>
    /// Exterior pass with an invisible slicing plane: geometry whose centroid lies on the
    /// <paramref name="cutNormal"/> side of <paramref name="cutOrigin"/> is not drawn (camera half-space culled).
    /// </summary>
    public static void Draw(CadDocument document, Vector3 cutOrigin, Vector3 cutNormal)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (cutNormal.LengthSquared() < 1e-8f)
        {
            Draw(document);
            return;
        }

        cutNormal = Vector3.Normalize(cutNormal);
        foreach (var entity in document.Entities)
            DrawOne(entity, cutOrigin, cutNormal);
    }

    /// <summary>Draw a single authored solid/mesh when it qualifies for the exterior pass.</summary>
    public static void DrawOne(CadEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (!IsExteriorDrawable(entity))
            return;
        DrawEntity(entity, cutaway: false, default, default);
    }

    /// <summary>Exterior solid with optional cutaway cull.</summary>
    public static void DrawOne(CadEntity entity, Vector3 cutOrigin, Vector3 cutNormal)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (!IsExteriorDrawable(entity))
            return;
        if (cutNormal.LengthSquared() < 1e-8f)
        {
            DrawEntity(entity, cutaway: false, default, default);
            return;
        }

        DrawEntity(entity, cutaway: true, cutOrigin, Vector3.Normalize(cutNormal));
    }

    /// <summary>Solids/meshes that belong in the exterior / Model sealed pass (not walls/spaces/ops).</summary>
    public static bool IsExteriorDrawable(CadEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var kind = entity.Kind.ToLowerInvariant();
        if (kind is not ("box" or "sphere" or "cylinder" or "cone" or "wedge" or "mesh"))
            return false;

        // Explicit interior-only props skip the sealed pass (still visible with Isolate ON).
        if (TryGetBoolProp(entity, "exterior") == false)
            return false;
        if (TryGetBoolProp(entity, "interiorOnly") == true)
            return false;

        // Sealed exterior = authored exterior solids only (explicit exterior=true / ext-* names).
        // Cargo peek (C40) stays interior-only unless a host marks exterior=true.
        if (TryGetBoolProp(entity, "exterior") == true)
            return true;
        if (IsPreservedExterior(entity))
            return true;
        return false;
    }

    /// <summary>Entities the RevG generator must not wipe on regenerate (hand-authored exterior).</summary>
    public static bool IsPreservedExterior(CadEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (TryGetBoolProp(entity, "exterior") == true)
            return true;
        var name = entity.Name ?? "";
        return name.StartsWith("ext-", StringComparison.OrdinalIgnoreCase)
               || name.StartsWith("nacelle-", StringComparison.OrdinalIgnoreCase);
    }

    private static void DrawEntity(CadEntity entity, bool cutaway, Vector3 cutOrigin, Vector3 cutNormal)
    {
        var color = ResolveColor(entity);
        switch (entity.Kind.ToLowerInvariant())
        {
            case "box" when CadShipGeometry.TryGetBox(entity, out var c, out var he):
                if (cutaway && CulledByCutPlane(c, cutOrigin, cutNormal))
                    return;
                World.DrawCube(c, MathF.Abs(he.X) * 2f, MathF.Abs(he.Y) * 2f, MathF.Abs(he.Z) * 2f, color);
                break;
            case "sphere" when entity.Center is not null:
            {
                var center = CadVec.To(entity.Center);
                if (cutaway && CulledByCutPlane(center, cutOrigin, cutNormal))
                    return;
                World.DrawSphere(center, entity.Radius, color);
                break;
            }
            case "cylinder" when entity.Center is not null:
            {
                var center = CadVec.To(entity.Center);
                if (cutaway && CulledByCutPlane(center, cutOrigin, cutNormal))
                    return;
                World.DrawCylinder(
                    center - new Vector3(0f, entity.Height * 0.5f, 0f),
                    entity.Radius,
                    entity.Radius,
                    entity.Height,
                    24,
                    color);
                break;
            }
            case "mesh":
            case "cone":
            case "wedge":
            {
                var mesh = CadSolidTessellator.TryTessellate(entity);
                if (mesh is null)
                    return;
                var edge = Color.FromArgb(255,
                    System.Math.Clamp(color.R - 40, 0, 255),
                    System.Math.Clamp(color.G - 40, 0, 255),
                    System.Math.Clamp(color.B - 40, 0, 255));
                for (var i = 0; i < mesh.Indices.Count; i += 3)
                {
                    var a = mesh.Vertices[mesh.Indices[i]];
                    var b = mesh.Vertices[mesh.Indices[i + 1]];
                    var c = mesh.Vertices[mesh.Indices[i + 2]];
                    if (cutaway)
                    {
                        // Keep a triangle if any vertex is on the keep side (not fully camera-side).
                        var ca = CulledByCutPlane(a, cutOrigin, cutNormal);
                        var cb = CulledByCutPlane(b, cutOrigin, cutNormal);
                        var cc = CulledByCutPlane(c, cutOrigin, cutNormal);
                        if (ca && cb && cc)
                            continue;
                    }

                    // Both windings so backfaces still read under orbit lighting.
                    World.DrawTriangle(a, b, c, color);
                    World.DrawTriangle(a, c, b, color);
                    World.DrawLine(a, b, edge);
                    World.DrawLine(b, c, edge);
                    World.DrawLine(c, a, edge);
                }

                break;
            }
        }
    }

    /// <summary>True when <paramref name="p"/> is on the camera / culled half-space of the cut plane.</summary>
    public static bool CulledByCutPlane(Vector3 p, Vector3 cutOrigin, Vector3 cutNormal) =>
        Vector3.Dot(p - cutOrigin, cutNormal) > 0f;

    private static Color ResolveColor(CadEntity entity)
    {
        if (entity.Name?.StartsWith("C40", StringComparison.OrdinalIgnoreCase) == true)
            return AccentCargo;
        if (entity.Color is { Length: >= 3 } rgb)
        {
            return Color.FromArgb(
                255,
                (int)(System.Math.Clamp(rgb[0], 0f, 1f) * 255),
                (int)(System.Math.Clamp(rgb[1], 0f, 1f) * 255),
                (int)(System.Math.Clamp(rgb[2], 0f, 1f) * 255));
        }

        return FallbackSolid;
    }

    private static bool? TryGetBoolProp(CadEntity entity, string key)
    {
        if (entity.Properties is null || !entity.Properties.TryGetValue(key, out var el))
            return null;
        return el.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(el.GetString(), out var b) => b,
            _ => null,
        };
    }
}
