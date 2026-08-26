using Silk.NET.OpenGL;

namespace Engine.Graphics;


/// Shared machinery for baking into a cubemap: render-target setup, the 6
/// face view matrices, and the unit cube used to rasterize each face. Used
/// by the equirect->cubemap conversion, irradiance convolution, and
/// specular prefilter passes — they differ only in shader and resolution.
public static class Cubemap {
    private static readonly Matrix4x4[] CaptureViews = {
        Matrix4x4.CreateLookAt(Vector3.Zero, new Vector3( 1, 0, 0), new Vector3(0, -1, 0)),
        Matrix4x4.CreateLookAt(Vector3.Zero, new Vector3(-1, 0, 0), new Vector3(0, -1, 0)),
        Matrix4x4.CreateLookAt(Vector3.Zero, new Vector3( 0, 1, 0), new Vector3(0, 0, 1)),
        Matrix4x4.CreateLookAt(Vector3.Zero, new Vector3( 0,-1, 0), new Vector3(0, 0,-1)),
        Matrix4x4.CreateLookAt(Vector3.Zero, new Vector3( 0, 0, 1), new Vector3(0, -1, 0)),
        Matrix4x4.CreateLookAt(Vector3.Zero, new Vector3( 0, 0,-1), new Vector3(0, -1, 0)),
    };

    public static readonly Matrix4x4 CaptureProjection =
        Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI/2f, 1f, 0.1f, 10f);

    /// Allocates an empty cubemap with `mipLevels` levels reserved (call
    /// with mipLevels=1 for the base equirect conversion and irradiance map,
    /// since neither needs roughness-driven mip sampling).
    public static uint CreateCubemap (GL gl, uint faceSize, int mipLevels, bool floatFormat = true) {
        uint handle = gl.GenTexture();
        gl.BindTexture(TextureTarget.TextureCubeMap, handle);

        for (int face = 0; face < 6; face++) {
            unsafe {
                gl.TexImage2D(
                    TextureTarget.TextureCubeMapPositiveX + face,
                    0,
                    floatFormat ? InternalFormat.Rgb16f : InternalFormat.Rgb8,
                    faceSize, faceSize, 0,
                    PixelFormat.Rgb,
                    floatFormat ? PixelType.Float : PixelType.UnsignedByte,
                    null);
            }
        }

        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter,
            (int)(mipLevels > 1 ? GLEnum.LinearMipmapLinear : GLEnum.Linear));
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)GLEnum.ClampToEdge);

        if (mipLevels > 1) {
            gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMaxLevel, mipLevels - 1);
            gl.GenerateMipmap(TextureTarget.TextureCubeMap);
        }

        gl.BindTexture(TextureTarget.TextureCubeMap, 0);
        return handle;
    }

    /// Renders `drawCubeFace` six times into `target`, once per cube face,
    /// at the given mip level. `drawCubeFace` is expected to bind its shader,
    /// set uView/uProjection, and issue the cube draw call — capture only
    /// owns the framebuffer/viewport/attachment setup.
    public static unsafe void RenderFaces (
        GL gl,
        uint target,
        uint faceSize,
        int mipLevel,
        Action<Matrix4x4, Matrix4x4, int> drawCubeFace) {

        uint fbo = gl.GenFramebuffer();
        uint rbo = gl.GenRenderbuffer();

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
        gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo);
        gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24, faceSize, faceSize);
        gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            RenderbufferTarget.Renderbuffer, rbo);

        gl.Viewport(0, 0, faceSize, faceSize);

        for (int face = 0; face < 6; face++) {
            gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.TextureCubeMapPositiveX + face,
                target,
                mipLevel);

            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            drawCubeFace(CaptureViews[face], CaptureProjection, face);
        }

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        gl.DeleteFramebuffer(fbo);
        gl.DeleteRenderbuffer(rbo);
    }
}