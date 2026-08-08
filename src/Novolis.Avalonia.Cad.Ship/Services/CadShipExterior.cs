using System.Drawing;
using System.Numerics;
using Novolis.Cad.Primitives;
using Novolis.Raylib.Rendering;

namespace Novolis.Avalonia.Cad.Ship.Services;

/// <summary>
/// Sealed freighter silhouette for decked ship <c>.cadjson</c> documents in Model view.
/// Tiered hull, aft ramp, and side pods (solid-orbit massing) — not dollhouse wall/space extrusions.
/// </summary>
public static class CadShipExterior
{
    private static readonly Vector3 LightDir = Vector3.Normalize(new Vector3(-0.25f, 0.85f, 0.35f));
    private static readonly Color AccentLight = Color.FromArgb(255, 210, 170, 90);
    private static readonly Color AccentCyan = Color.FromArgb(255, 90, 170, 190);
    private static readonly Color Steel = Color.FromArgb(255, 78, 86, 96);

    public static bool ShouldUseExterior(CadDocument document) =>
        CadVec.LooksLikeShipDocument(document);

    public static void Draw(CadDocument document)
    {
        _ = document;
        DrawOrbitShipShell();
        DrawOrbitExteriorGreebles();
        // CAD C40 boxes still help read the hold when present (nacelles owned by side pods).
        foreach (var entity in document.Entities)
        {
            if (entity.Kind != "box")
                continue;
            if (entity.Name?.Contains("nacelle", StringComparison.OrdinalIgnoreCase) == true)
                continue;
            if (entity.Name?.StartsWith("C40", StringComparison.OrdinalIgnoreCase) != true)
                continue;
            DrawC40(entity);
        }
    }

    private static void DrawC40(CadEntity box)
    {
        if (!CadShipGeometry.TryGetBox(box, out var c, out var he))
            return;
        var color = Color.FromArgb(255, 168, 128, 62);
        World.DrawCube(c, MathF.Abs(he.X) * 2f, MathF.Abs(he.Y) * 2f, MathF.Abs(he.Z) * 2f, Shade(color, 0.55f, 0.25f, Vector3.UnitY));
    }

    private static void DrawOrbitShipShell()
    {
        const float halfBeam = 9.9f;
        const float zBow = 32.5f;
        const float zStern = -32.5f;
        const float yKeel = -0.15f;
        const float yCrown = 11.95f;
        var midZ = (zBow + zStern) * 0.5f;
        var len = zBow - zStern;

        var hull = Shade(Color.FromArgb(255, 78, 84, 94), 0.42f, 0.55f, Vector3.UnitY);
        var hullDark = Shade(Color.FromArgb(255, 42, 46, 54), 0.5f, 0.45f, Vector3.UnitY);
        var cargoBand = Shade(Color.FromArgb(255, 58, 64, 72), 0.48f, 0.5f, Vector3.UnitY);
        var hab = Shade(Color.FromArgb(255, 110, 118, 128), 0.45f, 0.4f, Vector3.UnitY);
        var accentPlate = Shade(Color.FromArgb(255, 145, 150, 160), 0.5f, 0.35f, Vector3.UnitY);
        var glass = Color.FromArgb(240, 18, 32, 48);

        // Tiered massing: cargo lower / hab upper (not one grey brick).
        World.DrawCube(new Vector3(0f, 3.2f, midZ), halfBeam * 2f * 1.02f, 6.6f, len * 0.97f, cargoBand);
        World.DrawCube(new Vector3(0f, 8.6f, midZ + 2f), halfBeam * 2f * 0.88f, 4.4f, len * 0.72f, hab);
        World.DrawCube(new Vector3(0f, yKeel + 0.35f, midZ), halfBeam * 2f * 0.92f, 0.7f, len * 0.95f, hullDark);
        World.DrawCube(new Vector3(0f, yCrown - 0.2f, midZ + 2f), halfBeam * 2f * 0.9f, 0.4f, len * 0.74f, accentPlate);

        // Bridge tower + glass brow
        World.DrawCube(new Vector3(0f, 9.8f, zBow - 12f), 11.5f, 3.6f, 14f, accentPlate);
        World.DrawCube(new Vector3(0f, 10.6f, zBow - 5f), 9.5f, 2.2f, 6f, hab);
        World.DrawCube(new Vector3(0f, 6.0f, zBow - 1.2f), 12.5f, 4.2f, 3.2f, hull);
        World.DrawCube(new Vector3(0f, 6.2f, zBow - 0.15f), 10.5f, 2.2f, 0.22f, glass);
        for (var i = -3; i <= 3; i++)
            World.DrawCube(new Vector3(i * 1.45f, 6.2f, zBow - 0.05f), 0.1f, 2.2f, 0.12f, AccentCyan);

        // Aft cargo hatch-ramp house
        World.DrawCube(new Vector3(0f, 5.2f, zStern + 7f), halfBeam * 2f * 0.92f, 8.8f, 10f, hullDark);
        World.DrawCube(new Vector3(0f, 2.6f, zStern + 0.35f), 14.5f, 5.0f, 0.55f, Color.FromArgb(255, 22, 24, 28));
        World.DrawCube(new Vector3(0f, 5.2f, zStern + 0.15f), 15.2f, 0.35f, 0.35f, accentPlate);
        World.DrawCube(new Vector3(-7.4f, 2.6f, zStern + 0.15f), 0.35f, 5.0f, 0.35f, accentPlate);
        World.DrawCube(new Vector3(7.4f, 2.6f, zStern + 0.15f), 0.35f, 5.0f, 0.35f, accentPlate);
        World.DrawCube(new Vector3(0f, 0.2f, zStern + 0.15f), 15.2f, 0.35f, 0.35f, accentPlate);

        var rampFace = Shade(Color.FromArgb(255, 118, 108, 72), 0.5f, 0.35f, -Vector3.UnitZ);
        var rampDark = Shade(Color.FromArgb(255, 70, 64, 48), 0.55f, 0.3f, -Vector3.UnitZ);
        World.DrawCube(new Vector3(0f, 2.55f, zStern + 0.55f), 13.8f, 4.7f, 0.28f, rampFace);
        World.DrawCube(new Vector3(0f, 2.55f, zStern + 0.72f), 12.6f, 3.6f, 0.12f, rampDark);
        World.DrawCube(new Vector3(0f, 2.55f, zStern + 0.78f), 0.12f, 4.2f, 0.1f, Steel);
        World.DrawCube(new Vector3(-4.2f, 2.55f, zStern + 0.78f), 0.18f, 3.4f, 0.1f, Steel);
        World.DrawCube(new Vector3(4.2f, 2.55f, zStern + 0.78f), 0.18f, 3.4f, 0.1f, Steel);
        World.DrawCube(new Vector3(0f, 0.45f, zStern + 0.9f), 14.2f, 0.45f, 0.7f, hullDark);

        var amber = Shade(Color.FromArgb(255, 140, 110, 55), 0.55f, 0.25f, Vector3.UnitY);
        World.DrawCube(new Vector3(0f, 0.25f, zStern - 0.55f), 13.0f, 0.18f, 1.1f, amber);
        World.DrawCube(new Vector3(0f, 0.55f, zStern - 1.35f), 12.2f, 0.16f, 0.9f, amber);

        for (var i = -5; i <= 5; i++)
        {
            var z = midZ + i * (len * 0.08f);
            World.DrawCube(new Vector3(-halfBeam * 1.02f, 3.2f, z), 0.12f, 5.8f, 0.1f, hullDark);
            World.DrawCube(new Vector3(halfBeam * 1.02f, 3.2f, z), 0.12f, 5.8f, 0.1f, hullDark);
        }

        World.DrawCube(new Vector3(-halfBeam * 1.03f, 4.5f, midZ + 4f), 0.18f, 0.55f, 28f, AccentLight);
        World.DrawCube(new Vector3(halfBeam * 1.03f, 4.5f, midZ + 4f), 0.14f, 0.35f, 28f, AccentCyan);
        World.DrawCube(new Vector3(0f, 6.6f, midZ - 4f), halfBeam * 2.05f, 0.18f, 0.35f, AccentLight);

        World.DrawCylinder(new Vector3(0f, yCrown + 1.8f, zBow - 12f), 0.16f, 0.16f, 3.4f, 8, Steel);
        World.DrawSphere(new Vector3(0f, yCrown + 3.6f, zBow - 12f), 0.55f, AccentCyan);
        World.DrawCube(new Vector3(0f, yCrown + 0.4f, zBow - 12f), 2.2f, 0.45f, 2.2f, accentPlate);

        DrawOrbitSidePods(halfBeam, midZ, zStern);
    }

    private static void DrawOrbitSidePods(float halfBeam, float midZ, float zStern)
    {
        var pod = Shade(Color.FromArgb(255, 88, 94, 104), 0.4f, 0.55f, Vector3.UnitX);
        var podDark = Shade(Color.FromArgb(255, 40, 44, 52), 0.45f, 0.5f, Vector3.UnitX);
        var strut = Shade(Steel, 0.4f, 0.55f, Vector3.UnitY);
        var nozzle = Shade(Color.FromArgb(255, 48, 52, 58), 0.35f, 0.65f, -Vector3.UnitZ);
        var glow = Color.FromArgb(255, 255, 130, 40);
        var glowCore = Color.FromArgb(255, 255, 210, 120);
        var graviton = Shade(Color.FromArgb(255, 70, 120, 160), 0.35f, 0.55f, Vector3.UnitX);
        var gravitonCore = Color.FromArgb(220, 120, 210, 255);
        var coil = Shade(AccentCyan, 0.3f, 0.6f, Vector3.UnitZ);

        foreach (var side in new[] { -1f, 1f })
        {
            var x = side * halfBeam * 1.28f;
            World.DrawCube(new Vector3(side * halfBeam * 1.08f, 3.6f, midZ - 2f), 1.6f, 1.4f, 8f, strut);
            World.DrawCube(new Vector3(x, 3.4f, midZ - 2f), 3.4f, 4.2f, 18f, pod);
            World.DrawCube(new Vector3(x, 5.6f, midZ - 2f), 2.8f, 0.3f, 16f, AccentLight);

            var engZ = zStern + 4.5f;
            World.DrawCube(new Vector3(x, 3.2f, engZ + 1.2f), 3.0f, 3.6f, 4.5f, podDark);
            foreach (var (dy, dz) in new (float, float)[] { (-0.85f, 0f), (0.85f, 0f), (0f, 0f) })
            {
                World.DrawCube(new Vector3(x, 3.2f + dy, engZ - 0.4f + dz), 1.35f, 1.35f, 1.1f, nozzle);
                World.DrawCube(new Vector3(x, 3.2f + dy, engZ - 1.25f + dz), 1.0f, 1.0f, 0.7f, glow);
                World.DrawSphere(new Vector3(x, 3.2f + dy, engZ - 1.7f + dz), 0.32f, glowCore);
            }

            var ftlZ = midZ + 6f;
            World.DrawCube(new Vector3(x, 3.6f, ftlZ), 3.6f, 3.8f, 5.5f, podDark);
            World.DrawCube(new Vector3(x + side * 1.55f, 3.6f, ftlZ), 0.35f, 2.8f, 2.8f, graviton);
            World.DrawCube(new Vector3(x + side * 1.85f, 3.6f, ftlZ), 0.22f, 2.1f, 2.1f, coil);
            World.DrawCube(new Vector3(x + side * 2.05f, 3.6f, ftlZ), 0.18f, 1.4f, 1.4f, gravitonCore);
            for (var i = -2; i <= 2; i++)
            {
                World.DrawCube(new Vector3(x, 3.6f, ftlZ + i * 0.85f), 3.75f, 0.18f, 0.22f, coil);
                World.DrawCube(new Vector3(x, 3.6f + i * 0.55f, ftlZ), 3.75f, 0.16f, 0.16f, graviton);
            }

            World.DrawSphere(new Vector3(x + side * 2.35f, 3.6f, ftlZ), 0.45f, gravitonCore);
            World.DrawSphere(new Vector3(x + side * 2.55f, 3.6f, ftlZ), 0.22f, AccentCyan);
        }
    }

    private static void DrawOrbitExteriorGreebles()
    {
        const float zBow = 32.5f;
        const float zStern = -32.5f;
        var midZ = (zBow + zStern) * 0.5f;
        var plate = Shade(Color.FromArgb(255, 120, 126, 136), 0.5f, 0.4f, Vector3.UnitY);
        var dark = Shade(Color.FromArgb(255, 36, 40, 48), 0.45f, 0.5f, Vector3.UnitY);

        World.DrawCube(new Vector3(0f, 11.4f, midZ + 6f), 7.5f, 1.6f, 10f, plate);
        World.DrawCube(new Vector3(3.2f, 13.2f, zBow - 16f), 0.1f, 2.4f, 0.1f, Steel);
        World.DrawCube(new Vector3(-3.2f, 12.8f, zBow - 18f), 0.1f, 1.8f, 0.1f, Steel);
        World.DrawSphere(new Vector3(3.2f, 14.5f, zBow - 16f), 0.28f, AccentLight);
        World.DrawCube(new Vector3(0f, 10.2f, zStern + 8f), 8f, 0.7f, 6f, dark);
        World.DrawCube(new Vector3(0f, 10.6f, zStern + 6f), 3.5f, 0.45f, 2.5f, AccentLight);
    }

    private static Color Shade(Color c, float roughness, float metalness, Vector3 normal)
    {
        var n = Vector3.Normalize(normal);
        var ndl = System.Math.Clamp(Vector3.Dot(n, LightDir), 0.15f, 1f);
        var amb = 0.35f + (1f - roughness) * 0.1f;
        var lit = amb + ndl * (0.55f + metalness * 0.2f);
        return Color.FromArgb(
            c.A,
            (int)System.Math.Clamp(c.R * lit, 0, 255),
            (int)System.Math.Clamp(c.G * lit, 0, 255),
            (int)System.Math.Clamp(c.B * lit, 0, 255));
    }
}
