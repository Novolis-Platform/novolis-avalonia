using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Novolis.Avalonia._3D.Services;

/// <summary>Minimal rlgl immediate-mode line batching (compiled into raylib.dll).</summary>
internal static class RlglLines
{
    public const int Lines = 0x0001;

    [DllImport("raylib", EntryPoint = "rlBegin", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Begin(int mode);

    [DllImport("raylib", EntryPoint = "rlEnd", CallingConvention = CallingConvention.Cdecl)]
    public static extern void End();

    [DllImport("raylib", EntryPoint = "rlVertex3f", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Vertex3f(float x, float y, float z);

    [DllImport("raylib", EntryPoint = "rlColor4ub", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Color4ub(byte r, byte g, byte b, byte a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Color(Color color) =>
        Color4ub(color.R, color.G, color.B, color.A);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Vertex(Vector3 v) =>
        Vertex3f(v.X, v.Y, v.Z);
}
