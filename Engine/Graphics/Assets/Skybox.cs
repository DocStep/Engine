using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class Skybox : IDisposable {
    public Skybox (HdrTexture? texture) {
        GL = Renderer.GL;
        _emptyVao = GL.GenVertexArray();
        SetTexture(texture);
    }


    private readonly GL GL;
    //private readonly Shader _shader;

    public HdrTexture? texture { get; private set; }
    public float maxLod { get; private set; }
    public uint prefilteredHandle { get; private set; }
    private uint _emptyVao;

    public Material? material = null;


    public void SetTexture (HdrTexture? texture) {
        if (texture is null) return;

        this.texture = texture;

        PrefilterSkybox(texture, out uint handle, out int mipMaxLod);
        Log.log("mipMaxLod", mipMaxLod);
        prefilteredHandle = handle;
        maxLod = mipMaxLod;
    }

    public void Draw () {
        if (!Constants.renderSkybox) return;

        material = AssetsEngine._mat_Skybox;
        if (material is null) return;
        if (texture is null) return;

        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Front);
        GL.DepthMask(false);

        texture.Bind(TextureUnit.Texture0);
        
        material.shader.Use();
        material.Apply();

        GL.BindVertexArray(_emptyVao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        Renderer.Instance.Stats.DrawCalls++;

        GL.CullFace(TriangleFace.Back);
        GL.DepthMask(true);
        GL.DepthFunc(DepthFunction.Less);
    }


    public void PrefilterSkybox (HdrTexture source, out uint prefilteredHandle, out int maxLod) {
        GL gl = Renderer.GL;

        int baseW = source.Width, baseH = source.Height;
        int mipLevels = Math.Min((int)MathF.Floor(MathF.Log2(baseW)) + 1, 6);

        prefilteredHandle = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, prefilteredHandle);
        for (int level = 0; level < mipLevels; level++) {
            int w = Math.Max(1, baseW >> level);
            int h = Math.Max(1, baseH >> level);
            unsafe { gl.TexImage2D(TextureTarget.Texture2D, level, InternalFormat.Rgba16f, (uint)w, (uint)h, 0, PixelFormat.Rgba, PixelType.Float, null); }
        }
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, mipLevels - 1); /// tells the driver exactly how many levels exist — avoids relying on implicit chain-completeness rules

        uint fbo = gl.GenFramebuffer();
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

        Shader prefilterShader = AssetsEngine._shader_IBLPrefilter;
        for (int level = 0; level < mipLevels; level++) {
            int w = Math.Max(1, baseW >> level);
            int h = Math.Max(1, baseH >> level);
            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, prefilteredHandle, level);

            GLEnum fboStatus = gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (fboStatus != GLEnum.FramebufferComplete) {
                Log.log($"Prefilter FBO incomplete at level {level}: {fboStatus}", LogType.warning);
                continue;
            }

            gl.Viewport(0, 0, (uint)w, (uint)h);

            float roughness = mipLevels <= 1 ? 0f : (float)level/(mipLevels - 1);

            prefilterShader.Use();
            source.Bind(TextureUnit.Texture0);
            prefilterShader.SetInt(Shader.uEnvMap, 0);
            prefilterShader.SetFloat(Shader.uRoughness, roughness);
            prefilterShader.SetFloat("uResolutionX", source.Width);
            prefilterShader.SetFloat("uResolutionY", source.Height);
            /// uFireflyClamp tune — lower = smoother but dimmer sun bloom, higher = closer to true brightness but more residual fireflies
            prefilterShader.SetFloat("uFireflyClamp", 16f);
            gl.BindVertexArray(_emptyVao);
            gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
            Renderer.Instance.Stats.DrawCalls++;
        }

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        gl.DeleteFramebuffer(fbo);

        maxLod = mipLevels - 1;
    }


    public void Dispose () {
        GL.DeleteVertexArray(_emptyVao);
    }

}