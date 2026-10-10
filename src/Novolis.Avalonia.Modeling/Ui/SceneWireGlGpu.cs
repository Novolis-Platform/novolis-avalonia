using System.Numerics;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Novolis.Avalonia.Modeling.Services;
using Novolis.Avalonia.Modeling.Session;
using Novolis.Silk;

namespace Novolis.Avalonia.Modeling.Ui;

sealed class SceneWireGlGpu : ISceneWireGlGpu
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

    private readonly GlCommands _gl;
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
        _gl = GlCommands.FromProcAddress(glInterface.GetProcAddress);
        _program = _gl.CompileProgram(Vs, Fs);
        _uMvp = _gl.GetUniformLocation(_program, "uMvp");
        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();
        _gl.BindVertexArray(_vao);
        _gl.BindArrayBuffer(_vbo);
        const uint stride = 6 * sizeof(float);
        _gl.VertexAttribFloat(0, 3, stride, 0);
        _gl.VertexAttribFloat(1, 3, stride, 3 * sizeof(float));
        _gl.BindVertexArray(0);
    }

    public void Render(SceneSessionService session, SceneViewportCamera camera, int framebuffer, int w, int h, bool rebuildLines)
    {
        _gl.BindFramebuffer(framebuffer);
        _gl.Viewport(w, h);
        _gl.EnableDepthLequal();
        _gl.SetCullFace(false);
        _gl.ClearColorDepth(0.07f, 0.09f, 0.13f, 1f);

        camera.SyncActiveCamera();
        var mvp = camera.BuildViewProjection(w / (float)h);

        if (rebuildLines || _vertexCount == 0)
            RebuildLines(session);

        if (_vertexCount < 2)
            return;

        _gl.UseProgram(_program);
        _gl.UniformMatrix4(_uMvp, mvp);
        _gl.BindVertexArray(_vao);
        _gl.DrawLines(_vertexCount);
    }

    public void ReadRgba(Span<byte> rgba, int w, int h)
    {
        if (rgba.Length < w * h * 4)
            throw new ArgumentException("RGBA buffer too small.", nameof(rgba));
        _gl.ReadRgba(rgba, w, h);
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
        _gl.BindArrayBuffer(_vbo);
        _gl.BufferFloats(CollectionsMarshal.AsSpan(_floats));
    }
}
