using Silk.NET.OpenGL;

namespace Engine.Graphics;


/// GPU texture wrapper for an HDR (equirectangular) image
public class HdrTexture : IAsset<HdrTexture> {

    public string Name { get; protected set; } = string.Empty;

    public uint Handle { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }


    public static HdrTexture Load (string path) {
        GL gl = Renderer.GL;
        HdrLoader.Load(path, out float[] data, out int width, out int height);

        HdrTexture tex = new HdrTexture {
            Name = Path.GetFileName(path),
            Width = width,
            Height = height,
        };

        tex.Handle = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, tex.Handle);

        unsafe {
            fixed (float* d = data) {
                gl.TexImage2D(TextureTarget.Texture2D, level: 0, InternalFormat.Rgb32f, (uint)width, (uint)height,
                    border: 0, PixelFormat.Rgb, PixelType.Float, d);
            }
        }

        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

        gl.GenerateMipmap(TextureTarget.Texture2D);

        gl.BindTexture(TextureTarget.Texture2D, 0);

        return tex;
    }


    public void Bind (TextureUnit unit = TextureUnit.Texture0) {
        Renderer.GL.ActiveTexture(unit);
        Renderer.GL.BindTexture(TextureTarget.Texture2D, Handle);
    }


    public void Dispose () {
        Renderer.GL.DeleteTexture(Handle);
    }

}