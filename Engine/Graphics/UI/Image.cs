//using Engine.Graphics;
using Silk.NET.OpenGL;
using StbImageSharp;
using Newtonsoft.Json;

namespace Engine.Graphics.UI;

public class Image : Component {

    public override string Name => nameof(Image);

    [Hide][JsonIgnore] private GL GL => Renderer.GL;

    [Hide] public string Path { get; set; } = "";
    public Vector2 Size { get; set; } = new Vector2(100, 100);
    public Vector4 Tint { get; set; } = new Vector4(1f, 1f, 1f, 1f);

    [Hide][JsonIgnore] private uint _textureId;
    [Hide][JsonIgnore] private uint _vao, _vbo;
    [Hide][JsonIgnore] private bool _loaded = false;

    public override void OnAdd () {
        SetupBuffers();
        if (0 < Path.Length) Load(Path);
    }

    public void Load (string relativePath) {
        Path = System.IO.Path.Combine(AppContext.BaseDirectory, relativePath);

        ImageResult image = ImageResult.FromMemory(File.ReadAllBytes(Path), ColorComponents.RedGreenBlueAlpha);

        if (_textureId == 0) _textureId = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _textureId);

        unsafe {
            fixed (byte* ptr = image.Data) {
                GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)image.Width, (uint)image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }
        }

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

        _loaded = true;
    }

    private unsafe void SetupBuffers () {
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

    internal unsafe void Draw (Matrix4x4 projection) {
        if (!_loaded) return;

        Vector3 position = gameObject.Transform.Position;
        float x0 = position.X;
        float y0 = position.Y;
        float x1 = x0 + Size.X;
        float y1 = y0 + Size.Y;

        Vertex2D[] vertices = new Vertex2D[6] {
            new Vertex2D { Position = new Vector2(x0, y0), UV = new Vector2(0f, 0f) },
            new Vertex2D { Position = new Vector2(x1, y0), UV = new Vector2(1f, 0f) },
            new Vertex2D { Position = new Vector2(x1, y1), UV = new Vector2(1f, 1f) },

            new Vertex2D { Position = new Vector2(x0, y0), UV = new Vector2(0f, 0f) },
            new Vertex2D { Position = new Vector2(x1, y1), UV = new Vector2(1f, 1f) },
            new Vertex2D { Position = new Vector2(x0, y1), UV = new Vector2(0f, 1f) },
        };

        Material material = AssetsEngine._mat_UI;
        material.shader.Use();
        material.shader.SetMatrix4x4(Shader.Projection, projection);
        material.shader.SetVector4("uTint", Tint);
        material.Apply();

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _textureId);

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        fixed (Vertex2D* ptr = vertices) {
            GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length*sizeof(Vertex2D)), ptr, BufferUsageARB.StreamDraw);
        }

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        Renderer.Instance.Stats.DrawCallsUI++;
    }

    public override void OnRemove () {
        if (_vao != 0) GL.DeleteVertexArray(_vao);
        if (_vbo != 0) GL.DeleteBuffer(_vbo);
        if (_textureId != 0) GL.DeleteTexture(_textureId);
    }

}