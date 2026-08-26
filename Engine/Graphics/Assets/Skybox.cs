using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class Skybox : IDisposable {
    public Skybox (HdrTexture? texture) {
        GL = Renderer.GL;
        SetTexture(texture);

        _emptyVao = GL.GenVertexArray();
    }


    private readonly GL GL;
    //private readonly Shader _shader;

    public HdrTexture? texture { get; private set; }
    public float maxLod { get; private set; }
    //public bool isTextureValid => texture is not null;
    private uint _emptyVao;

    public Material? material = null;


    public void SetTexture (HdrTexture? texture) {
        if (texture is null) return;

        this.texture = texture;
        maxLod = MathF.Log2(MathF.Max(texture.Width, texture.Height));
    }

    public void Draw () {
        if (!Constants.renderSkybox) return;

        material = AssetsEngine._mat_Skybox;
        if (material is null) return;
        if (texture is null) return;

        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Front);
        GL.DepthMask(false);
        //GL.DepthFunc(DepthFunction.Lequal);

        texture.Bind(TextureUnit.Texture0);
        
        material.shader.Use();
        material.Apply();

        GL.BindVertexArray(_emptyVao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        Renderer.Instance.Stats.DrawCalls++;
        //GL.BindVertexArray(0);

        //GL.BindTexture(TextureTarget.TextureCubeMap, skyboxTextureId);

        GL.CullFace(TriangleFace.Back);
        GL.DepthMask(true);
        GL.DepthFunc(DepthFunction.Less);
    }


    public void Dispose () {
        GL.DeleteVertexArray(_emptyVao);
    }

}