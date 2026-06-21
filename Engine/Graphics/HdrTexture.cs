using Silk.NET.OpenGL;

namespace Engine.Graphics;


/// GPU-side equirectangular HDR texture. Plain RGB float texture — no
/// cubemap conversion, no mipmaps yet. Good enough for a skybox; IBL
/// convolution passes will build on top of this later.
public class HdrTexture : IDisposable {
    private readonly GL _gl;
    public readonly uint Handle;
    public readonly int Width;
    public readonly int Height;

    public HdrTexture (GL gl, string path) {
        _gl = gl;
        (float[] data, Width, Height) = HdrLoader.Load(path);

        Handle = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, Handle);

        unsafe {
            fixed (float* d = data) {
                _gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    InternalFormat.Rgb32f,
                    (uint)Width,
                    (uint)Height,
                    0,
                    PixelFormat.Rgb,
                    PixelType.Float,
                    d);
            }
        }

        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

        _gl.GenerateMipmap(TextureTarget.Texture2D);

        _gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void Bind (TextureUnit unit = TextureUnit.Texture0) {
        _gl.ActiveTexture(unit);
        _gl.BindTexture(TextureTarget.Texture2D, Handle);
    }

    public void Dispose () {
        _gl.DeleteTexture(Handle);
    }
}