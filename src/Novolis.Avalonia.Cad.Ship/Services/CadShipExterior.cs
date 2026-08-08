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

    /// <summary>Draw a single authored solid/mesh when it qualifies for the exterior pass.</summary>
    public static void DrawOne(CadEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (!IsExteriorDrawable(entity))
            return;
        DrawEntity(entity);
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

        // Sealed exterior = authored exterior solids (prop/name), plus C40 cargo peek when present.
        if (TryGetBoolProp(entity, "exterior") == true)
            return true;
        if (IsPreservedExterior(entity))
            return true;
        return entity.Name?.StartsWith("C40", StringComparison.OrdinalIgnoreCase) == true;
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

    private static void DrawEntity(CadEntity entity)
    {
        var color = ResolveColor(entity);
        switch (entity.Kind.ToLowerInvariant())
        {
            case "box" when CadShipGeometry.TryGetBox(entity, out var c, out var he):
                World.DrawCube(c, MathF.Abs(he.X) * 2f, MathF.Abs(he.Y) * 2f, MathF.Abs(he.Z) * 2f, color);
                break;
            case "sphere" when entity.Center is not null:
                World.DrawSphere(CadVec.To(entity.Center), entity.Radius, color);
                break;
            case "cylinder" when entity.Center is not null:
                World.DrawCylinder(
                    CadVec.To(entity.Center) - new Vector3(0f, entity.Height * 0.5f, 0f),
                    entity.Radius,
                    entity.Radius,
                    entity.Height,
                    24,
                    color);
                break;
            case "mesh":
            case "cone":
            case "wedge":
            {
                var mesh = CadSolidTessellator.TryTessellate(entity);
                if (mesh is null)
                    return;
                // Raylib World has no filled triangle mesh helper yet — edge wireframe of authored triangles.
                for (var i = 0; i < mesh.Indices.Count; i += 3)
                {
                    var a = mesh.Vertices[mesh.Indices[i]];
                    var b = mesh.Vertices[mesh.Indices[i + 1]];
                    var c = mesh.Vertices[mesh.Indices[i + 2]];
                    World.DrawLine(a, b, color);
                    World.DrawLine(b, c, color);
                    World.DrawLine(c, a, color);
                }

                break;
            }
        }
    }

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
