using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using Silk.NET.Maths;
using StbTrueTypeSharp;

namespace Engine.Graphics.UI;


public class TextRenderer {
    GL GL;
    FontAtlas _atlas;
    uint _vao, _vbo;
    Shader _shader;

    struct Vertex {
        public Vector2D<float> Pos;
        public Vector2D<float> UV;
    }

    List<Vertex> _vertices = new List<Vertex>();

    public unsafe TextRenderer () {
        this.GL = Renderer.Instance.GL;
        _atlas = FontAtlas.Load("src/Fonts/FuturaCyrillicMedium.ttf", 24);
        _shader = new Shader(Utils.LoadSrc("src/Shaders/UI/TextVertex.shader"), Utils.LoadSrc("src/Shaders/UI/TextFragment.shader"), "Text");

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        uint stride = (uint)(4*sizeof(float));
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, (void*)0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(2*sizeof(float)));
    }

    public unsafe void DrawText (string text, float x, float y, float screenWidth, float screenHeight) {
        _vertices.Clear();
        float cursorX = x;

        foreach (char c in text) {
            if (!_atlas.Chars.TryGetValue(c, out var ci)) continue;

            float x0 = cursorX + ci.XOffset;
            float y0 = y + ci.YOffset;
            float x1 = x0 + ci.Width;
            float y1 = y0 + ci.Height;

            /// two triangles per glyph
            _vertices.Add(new Vertex { Pos = new Vector2D<float>(x0, y0), UV = new Vector2D<float>(ci.U0, ci.V0) });
            _vertices.Add(new Vertex { Pos = new Vector2D<float>(x1, y0), UV = new Vector2D<float>(ci.U1, ci.V0) });
            _vertices.Add(new Vertex { Pos = new Vector2D<float>(x1, y1), UV = new Vector2D<float>(ci.U1, ci.V1) });

            _vertices.Add(new Vertex { Pos = new Vector2D<float>(x0, y0), UV = new Vector2D<float>(ci.U0, ci.V0) });
            _vertices.Add(new Vertex { Pos = new Vector2D<float>(x1, y1), UV = new Vector2D<float>(ci.U1, ci.V1) });
            _vertices.Add(new Vertex { Pos = new Vector2D<float>(x0, y1), UV = new Vector2D<float>(ci.U0, ci.V1) });

            cursorX += ci.XAdvance;
        }

        var ortho = Matrix4X4.CreateOrthographicOffCenter(0, screenWidth, screenHeight, 0, -1f, 1f);

        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _shader.Use();
        _shader.SetMatrix4X4("uProjection", ortho);
        _shader.SetInt("uTexture", 0);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _atlas.TextureId);

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        var arr = _vertices.ToArray();
        fixed (Vertex* ptr = arr) {
            GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(arr.Length*sizeof(Vertex)), ptr, BufferUsageARB.StreamDraw);
        }

        GL.DrawArrays(PrimitiveType.Triangles, 0, (uint)arr.Length);

        GL.Enable(EnableCap.DepthTest);
    }

    public unsafe void DrawDebugQuad (float screenWidth, float screenHeight) {
        GL.GetInteger(GetPName.DrawFramebufferBinding, out int fbo);
        Console.WriteLine($"Current FBO: {fbo}");

        var verts = new Vertex[] {
            new Vertex { Pos = new Vector2D<float>(100, 100), UV = new Vector2D<float>(0, 0) },
            new Vertex { Pos = new Vector2D<float>(300, 100), UV = new Vector2D<float>(1, 0) },
            new Vertex { Pos = new Vector2D<float>(300, 300), UV = new Vector2D<float>(1, 1) },
            new Vertex { Pos = new Vector2D<float>(100, 100), UV = new Vector2D<float>(0, 0) },
            new Vertex { Pos = new Vector2D<float>(300, 300), UV = new Vector2D<float>(1, 1) },
            new Vertex { Pos = new Vector2D<float>(100, 300), UV = new Vector2D<float>(0, 1) },
        };

        var ortho = Matrix4X4.CreateOrthographicOffCenter(0, screenWidth, screenHeight, 0, -1f, 1f);

        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _shader.Use();
        _shader.SetMatrix4X4("uProjection", ortho);
        _shader.SetInt("uTexture", 0);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _atlas.TextureId);

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        fixed (Vertex* ptr = verts) {
            GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(verts.Length*sizeof(Vertex)), ptr, BufferUsageARB.StreamDraw);
        }

        GL.DrawArrays(PrimitiveType.Triangles, 0, (uint)verts.Length);
        var err = GL.GetError();
        if (err != GLEnum.NoError) Console.WriteLine($"DrawDebugQuad Error: {err}");
        GL.Enable(EnableCap.DepthTest);
    }

}
