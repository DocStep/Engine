using Silk.NET.OpenGL;
using Silk.NET.Maths;

namespace Engine.Graphics;


public class Skybox : IDisposable {
    public Skybox (GL gl, Shader shader, HdrTexture? texture) {
        _gl = gl;
        _shader = shader;
        _texture = texture;

        _emptyVao = _gl.GenVertexArray();
    }


    private readonly GL _gl;
    private readonly Shader _shader;
    private readonly HdrTexture? _texture;
    private uint _emptyVao;

    public float BlurScale = 0f;


    public void Draw (Matrix4X4<float> view, Matrix4X4<float> projection) {
        if (_texture is null) return;

        Matrix4X4.Invert(view, out var invView);
        Matrix4X4.Invert(projection, out var invProjection);

        _gl.DepthFunc(DepthFunction.Lequal);
        _gl.DepthMask(false);

        _shader.Use();
        _shader.SetMatrix4("uInvView", Utils.MatrixToArray(invView));
        _shader.SetMatrix4("uInvProjection", Utils.MatrixToArray(invProjection));

        _texture.Bind(TextureUnit.Texture0);
        _shader.SetInt("uSkyboxTexture", 0);
        _shader.SetFloat("uBlurScale", BlurScale);

        _gl.BindVertexArray(_emptyVao);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
        _gl.BindVertexArray(0);

        _gl.DepthMask(true);
        _gl.DepthFunc(DepthFunction.Less);
    }

    public void Dispose () {
        _gl.DeleteVertexArray(_emptyVao);
    }
}