using Silk.NET.OpenGL;
using StbTrueTypeSharp;

namespace Engine.Graphics.UI;


public class FontAtlas : IDisposable {
    GL? GL = null;
    public uint TextureId;
    public int AtlasWidth;
    public int AtlasHeight;
    public Dictionary<char, CharInfo> Chars = new Dictionary<char, CharInfo>();
    public float FontSize;

    public struct CharInfo {
        public float U0, V0, U1, V1;
        public int Width, Height;
        public float XOffset, YOffset;
        public float XAdvance;
    }

    public static unsafe FontAtlas Load (string ttfPath, float fontSize) {
        GL gl = Renderer.Instance.GL;
        int atlasWidth = 512;
        int atlasHeight = 512;
        byte[] fontData = File.ReadAllBytes(ttfPath);
        FontAtlas atlas = new FontAtlas {
            GL = Renderer.Instance.GL,
            AtlasWidth = atlasWidth,
            AtlasHeight = atlasHeight,
            FontSize = fontSize,
        };

        StbTrueType.stbtt_bakedchar[] bakedChars = new StbTrueType.stbtt_bakedchar[96]; /// ASCII 32-127
        byte[] bitmap = new byte[atlasWidth*atlasHeight];

        fixed (byte* fontPtr = fontData)
        fixed (byte* bitmapPtr = bitmap)
        fixed (StbTrueType.stbtt_bakedchar* charsPtr = bakedChars) {
            StbTrueType.stbtt_BakeFontBitmap(fontPtr, 0, fontSize, bitmapPtr, atlasWidth, atlasHeight, 32, 96, charsPtr);
        }

        /// expand single-channel bitmap to RGBA so it samples correctly in the shader
        byte[] rgba = new byte[atlasWidth*atlasHeight*4];
        for (int i = 0; i < atlasWidth*atlasHeight; i++) {
            rgba[i*4+0] = 255;
            rgba[i*4+1] = 255;
            rgba[i*4+2] = 255;
            rgba[i*4+3] = bitmap[i];
        }

        atlas.TextureId = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, atlas.TextureId);
        fixed (byte* rgbaPtr = rgba) {
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)atlasWidth, (uint)atlasHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, rgbaPtr);
        }
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);

        for (int i = 0; i < 96; i++) {
            StbTrueType.stbtt_bakedchar bc = bakedChars[i];
            atlas.Chars[(char)(32 + i)] = new CharInfo {
                U0 = bc.x0 / (float)atlasWidth,
                V0 = bc.y0 / (float)atlasHeight,
                U1 = bc.x1 / (float)atlasWidth,
                V1 = bc.y1 / (float)atlasHeight,
                Width = bc.x1 - bc.x0,
                Height = bc.y1 - bc.y0,
                XOffset = bc.xoff,
                YOffset = bc.yoff,
                XAdvance = bc.xadvance
            };
        }

        return atlas;
    }

    public void Dispose () {
        GL?.DeleteTexture(TextureId);
    }

}
