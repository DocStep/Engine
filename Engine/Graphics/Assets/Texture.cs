using Engine.Graphics;
using Silk.NET.OpenGL;
using StbImageSharp;
using Newtonsoft.Json;

/// <summary>
/// GPU texture wrapper. Owns an OpenGL texture handle and can load pixel data from an image file on disk.
/// </summary>
public class Texture : IAsset<Texture> {

    public string Name { get; protected set; } = string.Empty;

    public uint Handle { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }


    /// <summary> Loads a texture from an image file (png/jpg/etc via StbImageSharp) and uploads it to the GPU. </summary>
    public static Texture Load (string path) {
        GL gl = Renderer.GL;

        Texture tex = new Texture();

        StbImage.stbi_set_flip_vertically_on_load(1);

        using FileStream stream = File.OpenRead(path);
        ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        tex.Width = image.Width;
        tex.Height = image.Height;
        tex.Handle = gl.GenTexture();

        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, tex.Handle);

        unsafe {
            fixed (byte* ptr = image.Data) {
                gl.TexImage2D(TextureTarget.Texture2D, level: 0, InternalFormat.Rgba8, (uint)image.Width, (uint)image.Height,
                    border: 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }
        }

        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
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