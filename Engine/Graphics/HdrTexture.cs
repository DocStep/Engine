using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class HdrTexture : IDisposable {
    public HdrTexture (string path) {
        GL = Renderer.GL;
        (float[] data, Width, Height) = HdrLoader.Load(path);

        Handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, Handle);

        unsafe {
            fixed (float* d = data) {
                GL.TexImage2D(
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

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

        GL.GenerateMipmap(TextureTarget.Texture2D);

        GL.BindTexture(TextureTarget.Texture2D, 0);
    }


    private readonly GL GL;
    public readonly uint Handle;
    public readonly int Width;
    public readonly int Height;


    public void Bind (TextureUnit unit = TextureUnit.Texture0) {
        GL.ActiveTexture(unit);
        GL.BindTexture(TextureTarget.Texture2D, Handle);
    }


    public void Dispose () {
        GL.DeleteTexture(Handle);
    }

}