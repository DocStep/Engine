using Silk.NET.OpenGL;

namespace Engine.Graphics;


public class HdrTexture : IDisposable {
    public HdrTexture (string path) {
        GL gl = Renderer.GL;
        HdrLoader.Load(path, out float[] data, out Width, out Height);

        Handle = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, Handle);

        unsafe {
            fixed (float* d = data) {
                gl.TexImage2D(TextureTarget.Texture2D, level: 0, InternalFormat.Rgb32f, (uint)Width, (uint)Height,
                    border:  0, PixelFormat.Rgb, PixelType.Float, d);
            }
        }

        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

        gl.GenerateMipmap(TextureTarget.Texture2D);

        gl.BindTexture(TextureTarget.Texture2D, 0);
    }


    public readonly uint Handle;
    public readonly int Width;
    public readonly int Height;


    public void Bind (TextureUnit unit = TextureUnit.Texture0) {
        Renderer.GL.ActiveTexture(unit);
        Renderer.GL.BindTexture(TextureTarget.Texture2D, Handle);
    }


    public void Dispose () {
        Renderer.GL.DeleteTexture(Handle);
    }

}