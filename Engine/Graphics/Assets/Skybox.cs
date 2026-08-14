using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class Skybox : IDisposable {
    public Skybox (Shader shader, HdrTexture? texture) {
        GL = Renderer.GL;
        _shader = shader;
        SetTexture(texture);

        _emptyVao = GL.GenVertexArray();
    }


    private readonly GL GL;
    private readonly Shader _shader;

    public HdrTexture? texture { get; private set; }
    public float maxLod { get; private set; }
    //public bool isTextureValid => texture is not null;

    private uint _emptyVao;

    public float blurScale = 0f;
    public const string BlurScale = "uBlurScale";


    public void SetTexture (HdrTexture? texture) {
        if (texture is null) return;

        this.texture = texture;
        maxLod = MathF.Log2(MathF.Max(texture.Width, texture.Height));
    }

    public void Draw (Matrix4x4 view, Matrix4x4 projection) {
        if (!Constants.renderSkybox) return;
        if (texture is null) return;

        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Front);
        GL.DepthMask(false);
        //GL.DepthFunc(DepthFunction.Lequal);

        Matrix4x4.Invert(view, out view);
        Matrix4x4.Invert(projection, out projection);
        texture.Bind(TextureUnit.Texture0);

        _shader.Use();
        _shader.SetMatrix4x4(Shader.View, view);
        _shader.SetMatrix4x4(Shader.Projection, projection);
        _shader.SetInt(Shader.Texture, 0);
        _shader.SetFloat(BlurScale, blurScale);

        GL.BindVertexArray(_emptyVao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        //GL.BindVertexArray(0);

        //GL.BindTexture(TextureTarget.TextureCubeMap, skyboxTextureId);

        GL.CullFace(TriangleFace.Back);
        GL.DepthMask(true);
        GL.DepthFunc(DepthFunction.Less);

        Renderer.Instance.Stats.DrawCalls++;
    }


    public void Dispose () {
        GL.DeleteVertexArray(_emptyVao);
    }

}