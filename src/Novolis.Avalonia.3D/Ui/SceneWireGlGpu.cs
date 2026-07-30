using System.Numerics;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Novolis.Avalonia._3D.Services;
using Novolis.Avalonia._3D.Session;
using Novolis.Avalonia.Rendering;
using Silk.NET.OpenGL;

namespace Novolis.Avalonia._3D.Ui;

/// <summary>Registers Silk GPU factory — loaded only when <see cref="SceneWireGlBootstrap"/> demands it.</summary>
internal static class SceneWireGlGpuFactory
{
    static SceneWireGlGpuFactory() =>
        SceneWireGlBootstrap.CreateImpl = static gl => new SceneWireGlGpu(gl);
}

file sealed class SceneWireGlGpu : ISceneWireGlGpu
{
    private const string Vs = """
        #version 330 core
        layout(location = 0) in vec3 aPos;
        layout(location = 1) in vec3 aColor;
        uniform mat4 uMvp;
        out vec3 vColor;
        void main() {
            vColor = aColor;
            gl_Position = uMvp * vec4(aPos, 1.0);
        }
        """;

    private const string Fs = """
        #version 330 core
        in vec3 vColor;
        out vec4 FragColor;
        void main() { FragColor = vec4(vColor, 1.0); }
        """;

    private readonly GL _gl;
    private readonly uint _program;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly int _uMvp;
    private readonly List<WireSegment> _segments = new(4096);
    private readonly List<float> _floats = new(4096 * 12);
    private int _vertexCount;
    private bool _disposed;

    public SceneWireGlGpu(GlInterface glInterface)
    {
        _gl = SilkGlBridge.CreateGl(glInterface);
        _program = Compile(_gl, Vs, Fs);
        _uMvp = _gl.GetUniformLocation(_program, "uMvp");
        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();
        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        unsafe
        {
            const uint stride = 6 * sizeof(float);
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, null);
            _gl.EnableVertexAttribArray(0);
            _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
            _gl.EnableVertexAttribArray(1);
        }

        _gl.BindVertexArray(0);
    }

    public void Render(SceneSessionService session, SceneViewportCamera camera, int framebuffer, int w, int h, bool rebuildLines)
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)framebuffer);
        _gl.Viewport(0, 0, (uint)w, (uint)h);
        _gl.Enable(EnableCap.DepthTest);
        _gl.DepthFunc(DepthFunction.Lequal);
        _gl.Disable(EnableCap.CullFace);
        _gl.ClearColor(0.07f, 0.09f, 0.13f, 1f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        camera.SyncActiveCamera();
        var mvp = camera.BuildViewProjection(w / (float)h);

        if (rebuildLines || _vertexCount == 0)
            RebuildLines(session);

        if (_vertexCount < 2)
            return;

        _gl.UseProgram(_program);
        unsafe
        {
            _gl.UniformMatrix4(_uMvp, 1, false, (float*)&mvp);
        }

        _gl.BindVertexArray(_vao);
        _gl.DrawArrays(PrimitiveType.Lines, 0, (uint)_vertexCount);
    }

    public void ReadRgba(Span<byte> rgba, int w, int h)
    {
        if (rgba.Length < w * h * 4)
            throw new ArgumentException("RGBA buffer too small.", nameof(rgba));
        unsafe
        {
            fixed (byte* p = rgba)
                _gl.ReadPixels(0, 0, (uint)w, (uint)h, PixelFormat.Rgba, PixelType.UnsignedByte, p);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteProgram(_program);
    }

    private void RebuildLines(SceneSessionService session)
    {
        WireSceneLineBuilder.Build(session, _segments);
        _floats.Clear();
        foreach (var seg in _segments)
        {
            var r = seg.R / 255f;
            var g = seg.G / 255f;
            var b = seg.Blue / 255f;
            _floats.Add(seg.A.X); _floats.Add(seg.A.Y); _floats.Add(seg.A.Z); _floats.Add(r); _floats.Add(g); _floats.Add(b);
            _floats.Add(seg.B.X); _floats.Add(seg.B.Y); _floats.Add(seg.B.Z); _floats.Add(r); _floats.Add(g); _floats.Add(b);
        }

        _vertexCount = _floats.Count / 6;
        if (_vertexCount < 2)
            return;
        Upload(CollectionsMarshal.AsSpan(_floats));
    }

    private unsafe void Upload(ReadOnlySpan<float> floats)
    {
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        fixed (float* p = floats)
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(floats.Length * sizeof(float)), p, BufferUsageARB.DynamicDraw);
    }

    private static uint Compile(GL gl, string vs, string fs)
    {
        var v = CompileShader(gl, ShaderType.VertexShader, vs);
        var f = CompileShader(gl, ShaderType.FragmentShader, fs);
        var p = gl.CreateProgram();
        gl.AttachShader(p, v);
        gl.AttachShader(p, f);
        gl.LinkProgram(p);
        gl.GetProgram(p, ProgramPropertyARB.LinkStatus, out var linked);
        if (linked == 0)
        {
            gl.GetProgramInfoLog(p, out var log);
            gl.DeleteProgram(p);
            gl.DeleteShader(v);
            gl.DeleteShader(f);
            throw new InvalidOperationException($"GL program link failed: {log}");
        }

        gl.DeleteShader(v);
        gl.DeleteShader(f);
        return p;
    }

    private static uint CompileShader(GL gl, ShaderType type, string src)
    {
        var s = gl.CreateShader(type);
        gl.ShaderSource(s, src);
        gl.CompileShader(s);
        gl.GetShader(s, ShaderParameterName.CompileStatus, out var ok);
        if (ok == 0)
        {
            gl.GetShaderInfoLog(s, out var log);
            gl.DeleteShader(s);
            throw new InvalidOperationException($"GL shader compile failed: {log}");
        }

        return s;
    }
}
