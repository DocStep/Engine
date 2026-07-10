using Silk.NET.OpenGL;

namespace Engine.Graphics.UI;


public class TextRenderer : IDisposable {
    public unsafe TextRenderer () {
        GL = Renderer.Instance.GL;
        _atlas = FontAtlas.Load("src/Fonts/FuturaCyrillicMedium.ttf", 24);
        _shader = new Shader(Utils.LoadSrc("src/Shaders/UI/Text_Vertex.shader"), Utils.LoadSrc("src/Shaders/UI/Text_Fragment.shader"), "Text");

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

    struct Vertex {
        public Vector2 Pos;
        public Vector2 UV;
    }

    GL GL;
    FontAtlas _atlas;
    uint _vao, _vbo;
    Shader _shader;

    List<Vertex> _vertices = new List<Vertex>();
    
    private readonly static List<TextRenderInfo> Texts = new List<TextRenderInfo>();
    public static void AddText (string _text) {
        Texts.Add(new (_text));
    }

    private bool _renderF3 = true;
    public bool renderF3 {
        get => _renderF3;
        set {
            if (_renderF3 != value) {
                _renderF3 = value;
            }
        }
    }


    internal void DrawUI () {
        if (Input.Inputs.Actions[Input.Inputs.F3].pressedDown) _renderF3 = !_renderF3;
        if (_renderF3) {
            GL.Disable(EnableCap.CullFace);

            int count = Texts.Count;
            int y = 0;
            int yStep = 20;
            for (int i = 0; i < count; i++) {
                y += yStep;
                DrawText(Texts[i].text, Constants.left, y);
            }
            Texts.Clear();
        }
    }

    public unsafe void DrawText (string text, float x, float y) {
        _vertices.Clear();
        float cursorX = x;
        float screenWidth = Engine.Window.Size.X;
        float screenHeight = Engine.Window.Size.Y;

        foreach (char c in text) {
            if (!_atlas.Chars.TryGetValue(c, out FontAtlas.CharInfo ci)) continue;

            float x0 = cursorX + ci.XOffset;
            float y0 = y + ci.YOffset;
            float x1 = x0 + ci.Width;
            float y1 = y0 + ci.Height;

            /// two triangles per glyph
            _vertices.Add(new Vertex { Pos = new Vector2(x0, y0), UV = new Vector2(ci.U0, ci.V0) });
            _vertices.Add(new Vertex { Pos = new Vector2(x1, y0), UV = new Vector2(ci.U1, ci.V0) });
            _vertices.Add(new Vertex { Pos = new Vector2(x1, y1), UV = new Vector2(ci.U1, ci.V1) });

            _vertices.Add(new Vertex { Pos = new Vector2(x0, y0), UV = new Vector2(ci.U0, ci.V0) });
            _vertices.Add(new Vertex { Pos = new Vector2(x1, y1), UV = new Vector2(ci.U1, ci.V1) });
            _vertices.Add(new Vertex { Pos = new Vector2(x0, y1), UV = new Vector2(ci.U0, ci.V1) });

            cursorX += ci.XAdvance;
        }

        Matrix4x4 ortho = Matrix4x4.CreateOrthographicOffCenter(0, screenWidth, screenHeight, 0, -1f, 1f);

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

        Vertex[] arr = _vertices.ToArray();
        fixed (Vertex* ptr = arr) {
            GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(arr.Length*sizeof(Vertex)), ptr, BufferUsageARB.StreamDraw);
        }

        GL.DrawArrays(PrimitiveType.Triangles, 0, (uint)arr.Length);

        GL.Enable(EnableCap.DepthTest);
    }


    public void Dispose () {
        GL.DeleteVertexArray(_vao);
        GL.DeleteBuffer(_vbo);
        _shader.Dispose();
        _atlas.Dispose();
    }

}
