using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Silk.NET.OpenGL;
using StbImageSharp;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Engine.Graphics;


/// <summary>
/// GPU texture wrapper. Owns an OpenGL texture handle and can load pixel data from an image file on disk.
/// </summary>
public class Texture : IAsset<Texture> {

    public string Name { get; set; } = string.Empty;
    public long Id { get; set; }
    public string? Path { get; set; }

    public uint Handle { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    [JsonIgnore, Hide]
    private static Texture? _white;
    [JsonIgnore, Hide]
    public static Texture White => _white ??= CreateSolid(255, 255, 255, 255);


    public void Bind (TextureUnit unit = TextureUnit.Texture0) {
        Renderer.GL.ActiveTexture(unit);
        Renderer.GL.BindTexture(TextureTarget.Texture2D, Handle);
    }

    private static Texture CreateSolid (byte r, byte g, byte b, byte a) {
        GL GL = Renderer.GL;
        Texture tex = new Texture { Name = "White", Width = 1, Height = 1 };
        tex.Handle = GL.GenTexture();

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, tex.Handle);

        byte[] pixel = { r, g, b, a };
        unsafe {
            fixed (byte* ptr = pixel)
                GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, 1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
        }

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        return tex;
    }


    /// <summary> Creates an empty depth texture to be used as a shadow map render target. </summary>
    public static Texture CreateDepth (int size) {
        GL gl = Renderer.GL;
        Texture tex = new Texture { Name = "ShadowMap", Width = size, Height = size };
        tex.Handle = gl.GenTexture();

        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, tex.Handle);

        unsafe {
            /// null data: the GPU fills it when we render the depth pass
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.DepthComponent24, (uint)size, (uint)size, 0, PixelFormat.DepthComponent, PixelType.Float, null);
        }

        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (int)GLEnum.CompareRefToTexture);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, (int)GLEnum.Lequal);

        /// Border depth 1.0 means "far away", so anything outside the shadow map is lit
        float[] border = { 1f, 1f, 1f, 1f };
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, border);

        gl.BindTexture(TextureTarget.Texture2D, 0);
        return tex;
    }



    /// Interface entry point, plain RGBA8 upload.
    public static Texture Load (string path, int part = 100) => LoadImage(path);
    
    /// Loads an image with options: sRGB decode, vertical flip, mipmaps.
    public static Texture LoadImage (string path, bool srgb = false, bool flipY = false, bool mips = false, bool mirrorX = false) {
        GL gl = Renderer.GL;

        Texture tex = new Texture { Name = System.IO.Path.GetFileName(path), Path = path };

        //long ms1 = Stopwatch.GetTimestamp();

        StbImage.stbi_set_flip_vertically_on_load(flipY ? 1 : 0); /// static flag, always set it explicitly
        using FileStream stream = File.OpenRead(path);
        ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        StbImage.stbi_set_flip_vertically_on_load(0);

        //long ms2 = Stopwatch.GetTimestamp();

        //if (mirrorX) {
        //    int w = image.Width, h = image.Height;
        //    byte[] bytes = image.Data;
        //    Parallel.For(0, h, y => {
        //        Span<uint> row = MemoryMarshal.Cast<byte, uint>(bytes.AsSpan(y*w*4, w*4));
        //        row.Reverse();
        //    });
        //}

        tex.Handle = gl.GenTexture();
        tex.Width = image.Width;
        tex.Height = image.Height;

        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, tex.Handle);

        InternalFormat format = srgb ? InternalFormat.Srgb8Alpha8 : InternalFormat.Rgba8;
        unsafe {
            fixed (byte* ptr = image.Data) {
                gl.TexImage2D(TextureTarget.Texture2D, level: 0, format, (uint)image.Width, (uint)image.Height,
                    border: 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }
        }

        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)(srgb ? GLEnum.ClampToEdge : GLEnum.Repeat));
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)(mips ? GLEnum.LinearMipmapLinear : GLEnum.Linear));
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        if (mips) gl.GenerateMipmap(TextureTarget.Texture2D);

        gl.BindTexture(TextureTarget.Texture2D, 0);

        //long ms3 = Stopwatch.GetTimestamp();
        //Log.log("LoadImage", path, Stopwatch.GetElapsedTime(ms1, ms2).TotalMilliseconds, Stopwatch.GetElapsedTime(ms2, ms3).TotalMilliseconds);

        return tex;
    }

    public void Save (string path) { }


    public void Dispose () {
        Renderer.GL.DeleteTexture(Handle);
    }

}