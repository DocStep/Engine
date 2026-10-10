using Silk.NET.OpenGL;

namespace Engine.Graphics;

public enum SkyMode {
    Texture = 0,
    Hdr = 1,
    Baked = 2,
}


public class Skybox : IDisposable {
    public Skybox (Texture? texture) {
        _emptyVao = Renderer.GL.GenVertexArray();
        SetTexture(texture);
    }
    public Skybox (HdrTexture? texture) {
        _emptyVao = Renderer.GL.GenVertexArray();
        SetTextureHdr(texture);
    }

    public SkyMode mode = SkyMode.Texture;

    [Hide] public Texture? Texture { get; private set; } = null;
    [Hide] public HdrTexture? HdrTexture { get; private set; } = null;
    [Hide] public SHAmbientProbe Probe { get; private set; }
    [Readonly] public float MaxLod { get; private set; }
    [Hide] public uint PrefilteredHandle { get; private set; }
    [Hide] private uint _emptyVao;

    public Material? material = null;

    [Hide, Newtonsoft.Json.JsonIgnore] public uint skyLdrHandle;

    /// Sets an LDR equirect texture (png/jpg) as the display sky. Lighting still comes from the HDR.
    /// The texture is owned by the caller (asset system), so Skybox doesn't dispose it.
    public void SetTexture (Texture? texture) {
        Texture = texture;
        if (texture is null) { mode = HdrTexture is null ? SkyMode.Hdr : SkyMode.Baked; return; }

        mode = SkyMode.Texture;
        if (HdrTexture is null) BuildIbl(texture.Handle, texture.Width, texture.Height); /// no HDR: light from the LDR sky
    }
    public void SetTextureHdr (HdrTexture? hdrTexture) {
        if (hdrTexture is null) return;
        HdrTexture = hdrTexture;
        BuildIbl(hdrTexture.Handle, hdrTexture.Width, hdrTexture.Height);
        BakeSky();
    }

    /// Builds reflections and ambient probe from any equirect GL texture (sRGB8 or float).
    private void BuildIbl (uint sourceHandle, int srcW, int srcH) {
        if (PrefilteredHandle != 0) Renderer.GL.DeleteTexture(PrefilteredHandle);

        PrefilterSkybox(sourceHandle, srcW, srcH, out uint handle, out int mipMaxLod);
        PrefilteredHandle = handle;
        MaxLod = mipMaxLod;

        Vector3[] pixels = ReadPixels(handle, mipMaxLod, out int w, out int h);
        Probe = BuildProbe(pixels, w, h);
    }

    /// Bakes the display sky from the .hdr on disk: exposure + PBR Neutral + sRGB 8-bit.
    /// Call again whenever HdrTexture.Exposure changes.
    public void BakeSky () {
        if (HdrTexture?.Path is null || !File.Exists(HdrTexture.Path)) return;
        GL gl = Renderer.GL;

        float[] data;
        int w, h;
        try {
            HdrLoader.Load(HdrTexture.Path, out data, out w, out h);
        } catch (Exception e) {
            Log.log($"BakeSky failed for \"{HdrTexture.Path}\": {e.Message}", LogType.warning);
            return;
        }

        byte[] pixels = HdrLoader.BakeLdrAces(data, w, h, HdrTexture.Exposure);

        if (skyLdrHandle != 0) gl.DeleteTexture(skyLdrHandle);
        skyLdrHandle = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, skyLdrHandle);
        unsafe {
            fixed (byte* p = pixels) {
                gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Srgb8Alpha8, (uint)w, (uint)h, 0, PixelFormat.Rgba, PixelType.UnsignedByte, p);
            }
        }
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        gl.GenerateMipmap(TextureTarget.Texture2D);
        gl.BindTexture(TextureTarget.Texture2D, 0);
    }


    public void Draw () {
        if (!Constants.renderSkybox) return;
        if (HdrTexture is null && Texture is null) return;

        /// fall back if the chosen source doesn't exist
        SkyMode m = mode;
        if (m == SkyMode.Texture && Texture is null) m = SkyMode.Baked;
        if (m == SkyMode.Baked && skyLdrHandle == 0) m = SkyMode.Hdr;
        if (m == SkyMode.Hdr && HdrTexture is null) m = SkyMode.Texture;
        if (m == SkyMode.Texture && Texture is null) return;

        material = m == SkyMode.Hdr ? AssetsEngine._mat_SkyboxHdr : AssetsEngine._mat_Skybox;
        if (material is null) return;

        GL gl = Renderer.GL;
        gl.Enable(EnableCap.CullFace);
        gl.CullFace(TriangleFace.Front);
        gl.DepthMask(false);

        if (m == SkyMode.Texture) Texture!.Bind(TextureUnit.Texture0);
        else if (m == SkyMode.Baked) {
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, skyLdrHandle);
        } else HdrTexture.Bind(TextureUnit.Texture0);

        material.shader.Use();
        material.Apply();
        if (m == SkyMode.Hdr) material.shader.SetFloat(Shader.uExposure, HdrTexture.Exposure);

        gl.BindVertexArray(_emptyVao);
        gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
        Renderer.Instance.Stats.DrawCalls++;

        gl.CullFace(TriangleFace.Back);
        gl.DepthMask(true);
        gl.DepthFunc(DepthFunction.Less);
    }


    public void PrefilterSkybox (uint sourceHandle, int srcW, int srcH, out uint prefilteredHandle, out int maxLod) {
        GL gl = Renderer.GL;

        int baseW = srcW, baseH = srcH;
        int mipLevels = Math.Min((int)MathF.Floor(MathF.Log2(baseW)) + 1, 6);

        prefilteredHandle = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, prefilteredHandle);
        for (int level = 0; level < mipLevels; level++) {
            int w = Math.Max(1, baseW >> level);
            int h = Math.Max(1, baseH >> level);
            unsafe {
                gl.TexImage2D(TextureTarget.Texture2D, level, InternalFormat.Rgba16f, 
                    (uint)w, (uint)h, 0, PixelFormat.Rgba, PixelType.Float, null);
            }
        }
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        /// tells the driver exactly how many levels exist — avoids relying on implicit chain-completeness rules
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, mipLevels - 1); 

        uint fbo = gl.GenFramebuffer();
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

        Shader prefilterShader = AssetsEngine._sh_IBLPrefilter;
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
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, sourceHandle);
            prefilterShader.SetInt(Shader.uEnvMap, 0);
            prefilterShader.SetFloat(Shader.uRoughness, roughness);
            prefilterShader.SetFloat("uResolutionX", srcW);
            prefilterShader.SetFloat("uResolutionY", srcH);
            int samples = roughness < 0.3f ? 128 : roughness < 0.6f ? 512 : 1024;
            prefilterShader.SetInt("uSampleCount", samples); // or SetInt if you have no uint setter
            //prefilterShader.SetFloat("uFireflyClamp", 1000000f);
            gl.BindVertexArray(_emptyVao);
            gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
            Renderer.Instance.Stats.DrawCalls++;
        }

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        gl.DeleteFramebuffer(fbo);

        maxLod = mipLevels - 1;
    }


    public static unsafe Vector3[] ReadPixels (uint handle, int level, out int w, out int h) {
        GL gl = Renderer.GL;
        gl.BindTexture(TextureTarget.Texture2D, handle);
        gl.GetTexLevelParameter(TextureTarget.Texture2D, level, GetTextureParameter.TextureWidth, out w);
        gl.GetTexLevelParameter(TextureTarget.Texture2D, level, GetTextureParameter.TextureHeight, out h);

        float[] data = new float[w*h*4];
        fixed (float* p = data) {
            gl.GetTexImage(TextureTarget.Texture2D, level, PixelFormat.Rgba, PixelType.Float, p);
        }

        Vector3[] pixels = new Vector3[w*h];
        for (int i = 0; i < pixels.Length; i++) {
            pixels[i] = new Vector3(data[i*4], data[i*4 + 1], data[i*4 + 2]);
        }
        return pixels;
    }

    public static SHAmbientProbe BuildProbe (Vector3[] pixels, int w, int h) {
        ProjectSH(pixels, w, h, out var Ar, out var Br, out var Cr, 0);
        ProjectSH(pixels, w, h, out var Ag, out var Bg, out var Cg, 1);
        ProjectSH(pixels, w, h, out var Ab, out var Bb, out var Cb, 2);
        return new SHAmbientProbe {
            SHAr = Ar,
            SHAg = Ag,
            SHAb = Ab,
            SHBr = Br,
            SHBg = Bg,
            SHBb = Bb,
            SHC = new Vector4(Cr.X, Cg.X, Cb.X, 1f),
            Intensity = 1f
        };
    }

    /// pixels: equirect RGB (use a low mip, e.g. 64x32), y is up, same mapping as SampleSphericalMap
    public static void ProjectSH (Vector3[] pixels, int w, int h, out Vector4 A, out Vector4 B, out Vector3 C, int channel) {
        Span<float> L = stackalloc float[9];
        L.Clear();
        for (int j = 0; j < h; j++) {
            float lat = ((j + 0.5f)/h - 0.5f)*MathF.PI;
            float dOmega = MathF.Cos(lat)*(MathF.PI/h)*(2f*MathF.PI/w);
            for (int i = 0; i < w; i++) {
                float phi = ((i + 0.5f)/w - 0.5f)*2f*MathF.PI;
                float x = MathF.Cos(lat)*MathF.Cos(phi), y = MathF.Sin(lat), z = MathF.Cos(lat)*MathF.Sin(phi);
                Vector3 p = pixels[j*w + i];
                float c = channel == 0 ? p.X : channel == 1 ? p.Y : p.Z;
                float wgt = c*dOmega;
                L[0] += wgt*0.282095f;
                L[1] += wgt*0.488603f*y;
                L[2] += wgt*0.488603f*z;
                L[3] += wgt*0.488603f*x;
                L[4] += wgt*1.092548f*x*y;
                L[5] += wgt*1.092548f*y*z;
                L[6] += wgt*0.315392f*(3f*z*z - 1f);
                L[7] += wgt*1.092548f*x*z;
                L[8] += wgt*0.546274f*(x*x - y*y);
            }
        }
        const float a1 = 2f/3f, a2 = 0.25f;   /// cosine lobe factors divided by PI
        A = new Vector4(a1*L[3]*0.488603f, a1*L[1]*0.488603f, a1*L[2]*0.488603f, L[0]*0.282095f - a2*L[6]*0.315392f);
        B = new Vector4(a2*L[4]*1.092548f, a2*L[5]*1.092548f, a2*L[6]*0.315392f*3f, a2*L[7]*1.092548f);
        C = new Vector3(a2*L[8]*0.546274f);
    }


    public void Dispose () {
        if (PrefilteredHandle != 0) Renderer.GL.DeleteTexture(PrefilteredHandle);
        if (skyLdrHandle != 0) Renderer.GL.DeleteTexture(skyLdrHandle);
        Renderer.GL.DeleteVertexArray(_emptyVao);
    }

}