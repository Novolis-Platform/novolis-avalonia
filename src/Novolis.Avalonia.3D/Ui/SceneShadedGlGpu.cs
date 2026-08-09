using System.Numerics;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Novolis.Avalonia._3D.Services;
using Novolis.Avalonia._3D.Session;
using Novolis.Avalonia.Rendering;
using Novolis._3D;
using Silk.NET.OpenGL;

namespace Novolis.Avalonia._3D.Ui;

/// <summary>Registers Silk shaded GPU factory — loaded only when OpenGL init demands it.</summary>
internal static class SceneShadedGlGpuFactory
{
    static SceneShadedGlGpuFactory() =>
        SceneShadedGlBootstrap.CreateImpl = static gl => new SceneShadedGlGpu(gl);
}

file sealed class SceneShadedGlGpu : ISceneShadedGlGpu
{
    private const int MaxLights = 8;

    private const string MeshVs = """
        #version 330 core
        layout(location = 0) in vec3 aPos;
        layout(location = 1) in vec3 aNormal;
        layout(location = 2) in vec3 aColor;
        uniform mat4 uMvp;
        out vec3 vWorldPos;
        out vec3 vNormal;
        out vec3 vColor;
        void main() {
            vWorldPos = aPos;
            vNormal = aNormal;
            vColor = aColor;
            gl_Position = uMvp * vec4(aPos, 1.0);
        }
        """;

    private const string MeshFs = """
        #version 330 core
        in vec3 vWorldPos;
        in vec3 vNormal;
        in vec3 vColor;
        out vec4 FragColor;
        uniform vec3 uAmbient;
        uniform float uExposure;
        uniform vec3 uEye;
        uniform int uLightCount;
        uniform vec3 uLightPos[8];
        uniform vec3 uLightColor[8];
        uniform float uLightIntensity[8];
        uniform float uLightDirectional[8];
        void main() {
            vec3 n = normalize(vNormal);
            if (!gl_FrontFacing) n = -n;
            vec3 lit = uAmbient;
            for (int i = 0; i < uLightCount; i++) {
                vec3 L;
                float atten = 1.0;
                if (uLightDirectional[i] > 0.5) {
                    L = normalize(-uLightPos[i]);
                } else {
                    vec3 toL = uLightPos[i] - vWorldPos;
                    float dist = max(length(toL), 0.001);
                    L = toL / dist;
                    atten = 1.0 / (1.0 + 0.09 * dist + 0.032 * dist * dist);
                }
                float ndl = max(dot(n, L), 0.0);
                float spec = pow(max(dot(reflect(-L, n), normalize(uEye - vWorldPos)), 0.0), 32.0) * 0.25;
                lit += uLightColor[i] * uLightIntensity[i] * atten * (ndl + spec);
            }
            vec3 rgb = vColor * lit * uExposure;
            FragColor = vec4(clamp(rgb, 0.0, 1.0), 1.0);
        }
        """;

    private const string WireVs = """
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

    private const string WireFs = """
        #version 330 core
        in vec3 vColor;
        out vec4 FragColor;
        void main() { FragColor = vec4(vColor, 0.55); }
        """;

    private readonly GL _gl;
    private readonly uint _meshProgram;
    private readonly uint _wireProgram;
    private readonly uint _meshVao;
    private readonly uint _meshVbo;
    private readonly uint _wireVao;
    private readonly uint _wireVbo;
    private readonly int _uMvpMesh;
    private readonly int _uAmbient;
    private readonly int _uExposure;
    private readonly int _uEye;
    private readonly int _uLightCount;
    private readonly int _uMvpWire;
    private readonly int[] _uLightPosLoc = new int[MaxLights];
    private readonly int[] _uLightColorLoc = new int[MaxLights];
    private readonly int[] _uLightIntensityLoc = new int[MaxLights];
    private readonly int[] _uLightDirectionalLoc = new int[MaxLights];
    private readonly List<float> _meshFloats = new(65536);
    private readonly List<float> _wireFloats = new(8192);
    private readonly List<WireSegment> _segments = new(4096);
    private int _meshVertexCount;
    private int _wireVertexCount;
    private bool _disposed;

    public SceneShadedGlGpu(GlInterface glInterface)
    {
        _gl = SilkGlBridge.CreateGl(glInterface);
        _meshProgram = Compile(_gl, MeshVs, MeshFs);
        _wireProgram = Compile(_gl, WireVs, WireFs);
        _uMvpMesh = _gl.GetUniformLocation(_meshProgram, "uMvp");
        _uAmbient = _gl.GetUniformLocation(_meshProgram, "uAmbient");
        _uExposure = _gl.GetUniformLocation(_meshProgram, "uExposure");
        _uEye = _gl.GetUniformLocation(_meshProgram, "uEye");
        _uLightCount = _gl.GetUniformLocation(_meshProgram, "uLightCount");
        for (var i = 0; i < MaxLights; i++)
        {
            _uLightPosLoc[i] = _gl.GetUniformLocation(_meshProgram, $"uLightPos[{i}]");
            _uLightColorLoc[i] = _gl.GetUniformLocation(_meshProgram, $"uLightColor[{i}]");
            _uLightIntensityLoc[i] = _gl.GetUniformLocation(_meshProgram, $"uLightIntensity[{i}]");
            _uLightDirectionalLoc[i] = _gl.GetUniformLocation(_meshProgram, $"uLightDirectional[{i}]");
        }

        _uMvpWire = _gl.GetUniformLocation(_wireProgram, "uMvp");

        _meshVao = _gl.GenVertexArray();
        _meshVbo = _gl.GenBuffer();
        _gl.BindVertexArray(_meshVao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _meshVbo);
        unsafe
        {
            const uint stride = 9 * sizeof(float);
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, null);
            _gl.EnableVertexAttribArray(0);
            _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));
            _gl.EnableVertexAttribArray(1);
            _gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, stride, (void*)(6 * sizeof(float)));
            _gl.EnableVertexAttribArray(2);
        }

        _wireVao = _gl.GenVertexArray();
        _wireVbo = _gl.GenBuffer();
        _gl.BindVertexArray(_wireVao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _wireVbo);
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

    public void Render(
        SceneSessionService session,
        SceneViewportCamera camera,
        SceneRenderSettings settings,
        int framebuffer,
        int w,
        int h,
        bool rebuildMesh)
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)framebuffer);
        _gl.Viewport(0, 0, (uint)w, (uint)h);
        _gl.Enable(EnableCap.DepthTest);
        _gl.DepthFunc(DepthFunction.Lequal);
        if (settings.TwoSided)
            _gl.Disable(EnableCap.CullFace);
        else
            _gl.Enable(EnableCap.CullFace);

        var clear = settings.ClearColor;
        _gl.ClearColor(clear.X, clear.Y, clear.Z, 1f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        camera.SyncActiveCamera();
        var eye = camera.Orbit.BuildEyePosition();
        var mvp = camera.BuildViewProjection(w / (float)h);

        if (rebuildMesh || _meshVertexCount == 0)
            RebuildMeshes(session, settings.BaseColor);

        if (_meshVertexCount >= 3)
        {
            _gl.UseProgram(_meshProgram);
            unsafe
            {
                _gl.UniformMatrix4(_uMvpMesh, 1, false, (float*)&mvp);
            }

            var amb = settings.EffectiveAmbient;
            _gl.Uniform3(_uAmbient, amb.X, amb.Y, amb.Z);
            _gl.Uniform1(_uExposure, settings.Exposure);
            _gl.Uniform3(_uEye, eye.X, eye.Y, eye.Z);
            UploadLights(session, settings.LightScale);

            _gl.BindVertexArray(_meshVao);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)_meshVertexCount);
        }

        if (settings.WireOverlay)
        {
            RebuildWire(session);
            if (_wireVertexCount >= 2)
            {
                _gl.Enable(EnableCap.Blend);
                _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                _gl.UseProgram(_wireProgram);
                unsafe
                {
                    _gl.UniformMatrix4(_uMvpWire, 1, false, (float*)&mvp);
                }

                _gl.BindVertexArray(_wireVao);
                _gl.DrawArrays(PrimitiveType.Lines, 0, (uint)_wireVertexCount);
                _gl.Disable(EnableCap.Blend);
            }
        }
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
        _gl.DeleteBuffer(_meshVbo);
        _gl.DeleteVertexArray(_meshVao);
        _gl.DeleteBuffer(_wireVbo);
        _gl.DeleteVertexArray(_wireVao);
        _gl.DeleteProgram(_meshProgram);
        _gl.DeleteProgram(_wireProgram);
    }

    private void UploadLights(SceneSessionService session, float lightScale)
    {
        Span<Vector3> pos = stackalloc Vector3[MaxLights];
        Span<Vector3> col = stackalloc Vector3[MaxLights];
        Span<float> inten = stackalloc float[MaxLights];
        Span<float> dir = stackalloc float[MaxLights];
        var count = 0;

        foreach (var ev in session.Evaluator.Cache.Lights)
        {
            if (count >= MaxLights) break;
            if (ev.Source is not LightNode light || !light.Enabled) continue;

            var color = new Vector3(
                light.Color.Length > 0 ? light.Color[0] : 1f,
                light.Color.Length > 1 ? light.Color[1] : 1f,
                light.Color.Length > 2 ? light.Color[2] : 1f);
            col[count] = color;
            inten[count] = System.Math.Max(0f, light.Intensity * lightScale);

            if (light.LightKind == LightKind.Infinite)
            {
                var forward = Vector3.TransformNormal(-Vector3.UnitZ, ev.WorldMatrix);
                if (forward.LengthSquared() < 1e-8f)
                    forward = -Vector3.UnitY;
                else
                    forward = Vector3.Normalize(forward);
                pos[count] = forward;
                dir[count] = 1f;
            }
            else
            {
                pos[count] = ev.WorldPosition;
                dir[count] = 0f;
            }

            count++;
        }

        if (count == 0)
        {
            // Default studio key so empty Look still shades.
            pos[0] = Vector3.Normalize(new Vector3(-0.45f, -0.75f, -0.4f));
            col[0] = Vector3.One;
            inten[0] = 1.4f * lightScale;
            dir[0] = 1f;
            count = 1;
        }

        _gl.Uniform1(_uLightCount, count);
        for (var i = 0; i < count; i++)
        {
            _gl.Uniform3(_uLightPosLoc[i], pos[i].X, pos[i].Y, pos[i].Z);
            _gl.Uniform3(_uLightColorLoc[i], col[i].X, col[i].Y, col[i].Z);
            _gl.Uniform1(_uLightIntensityLoc[i], inten[i]);
            _gl.Uniform1(_uLightDirectionalLoc[i], dir[i]);
        }
    }

    private void RebuildMeshes(SceneSessionService session, Vector3 baseColor)
    {
        _meshFloats.Clear();
        foreach (var mesh in session.Evaluator.Cache.EvaluatedMeshes)
        {
            var color = ResolveColor(session, mesh.SourceId, baseColor);
            var verts = mesh.Vertices;
            var idx = mesh.Indices;
            var world = mesh.World;
            for (var t = 0; t + 2 < idx.Length; t += 3)
            {
                var i0 = idx[t];
                var i1 = idx[t + 1];
                var i2 = idx[t + 2];
                if ((uint)i0 >= (uint)verts.Length || (uint)i1 >= (uint)verts.Length || (uint)i2 >= (uint)verts.Length)
                    continue;

                var a = Vector3.Transform(verts[i0], world);
                var b = Vector3.Transform(verts[i1], world);
                var c = Vector3.Transform(verts[i2], world);
                var n = Vector3.Cross(b - a, c - a);
                if (n.LengthSquared() > 1e-12f)
                    n = Vector3.Normalize(n);
                else
                    n = Vector3.UnitY;

                PushVert(a, n, color);
                PushVert(b, n, color);
                PushVert(c, n, color);
            }
        }

        _meshVertexCount = _meshFloats.Count / 9;
        if (_meshVertexCount < 3)
            return;
        Upload(_meshVbo, CollectionsMarshal.AsSpan(_meshFloats));
    }

    private void RebuildWire(SceneSessionService session)
    {
        WireSceneLineBuilder.Build(session, _segments, gridHalf: 0);
        _wireFloats.Clear();
        foreach (var seg in _segments)
        {
            var r = seg.R / 255f;
            var g = seg.G / 255f;
            var b = seg.Blue / 255f;
            _wireFloats.Add(seg.A.X); _wireFloats.Add(seg.A.Y); _wireFloats.Add(seg.A.Z);
            _wireFloats.Add(r); _wireFloats.Add(g); _wireFloats.Add(b);
            _wireFloats.Add(seg.B.X); _wireFloats.Add(seg.B.Y); _wireFloats.Add(seg.B.Z);
            _wireFloats.Add(r); _wireFloats.Add(g); _wireFloats.Add(b);
        }

        _wireVertexCount = _wireFloats.Count / 6;
        if (_wireVertexCount < 2)
            return;
        Upload(_wireVbo, CollectionsMarshal.AsSpan(_wireFloats));
    }

    private void PushVert(Vector3 p, Vector3 n, Vector3 c)
    {
        _meshFloats.Add(p.X); _meshFloats.Add(p.Y); _meshFloats.Add(p.Z);
        _meshFloats.Add(n.X); _meshFloats.Add(n.Y); _meshFloats.Add(n.Z);
        _meshFloats.Add(c.X); _meshFloats.Add(c.Y); _meshFloats.Add(c.Z);
    }

    private static Vector3 ResolveColor(SceneSessionService session, Guid sourceId, Vector3 fallback)
    {
        if (session.Document.Find(sourceId) is MeshNode mesh && mesh.MaterialId is { } mid
            && session.Document.Find(mid) is MaterialNode mat && mat.Color.Length >= 3)
        {
            return new Vector3(mat.Color[0], mat.Color[1], mat.Color[2]);
        }

        return fallback;
    }

    private unsafe void Upload(uint vbo, ReadOnlySpan<float> floats)
    {
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
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
