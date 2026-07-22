using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class Skybox : IDisposable {
    public Skybox (Shader shader, HdrTexture? texture) {
        GL = Renderer.GL;
        _shader = shader;
        _texture = texture;

        _emptyVao = GL.GenVertexArray();
    }


    private readonly GL GL;
    private readonly Shader _shader;
    private readonly HdrTexture? _texture;
    private uint _emptyVao;

    public float BlurScale = 0f;


    public void Draw (Matrix4x4 view, Matrix4x4 projection) {
        if (_texture is null) return;

        Matrix4x4.Invert(view, out Matrix4x4 invView);
        Matrix4x4.Invert(projection, out Matrix4x4 invProjection);

        GL.DepthFunc(DepthFunction.Lequal);
        GL.DepthMask(false);

        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Front);

        _shader.Use();
        _shader.SetMatrix4("uView", Matrix4x4.ToArray(invView));
        _shader.SetMatrix4("uProjection", Matrix4x4.ToArray(invProjection));

        _texture.Bind(TextureUnit.Texture0);
        _shader.SetInt("uTexture", 0);
        _shader.SetFloat("uBlurScale", BlurScale);

        GL.BindVertexArray(_emptyVao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        GL.BindVertexArray(0);

        //GL.BindTexture(TextureTarget.TextureCubeMap, skyboxTextureId);
        //GL.GenerateMipmap(TextureTarget.TextureCubeMap);

        GL.DepthMask(true);
        GL.DepthFunc(DepthFunction.Less);
    }


    public void Dispose () {
        GL.DeleteVertexArray(_emptyVao);
    }

}