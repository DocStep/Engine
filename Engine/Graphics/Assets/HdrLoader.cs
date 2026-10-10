using System.Threading.Tasks;

namespace Engine.Graphics;


/// Loads Radiance (.hdr) images — the de-facto standard format for HDR
/// environment maps (Poly Haven, etc.). Returns flat linear-RGB float data
/// ready to upload as a GL texture; no cubemap conversion happens here,
/// the image stays equirectangular.
public static class HdrLoader {
    public static void Load (string path, out float[] Data, out int Width, out int Height, bool mirrorX = true, float maxValue = 64f) {
        using FileStream stream = File.OpenRead(path);

        Width = Height = 0;
        ReadHeader(stream, out Width, out Height);

        Data = new float[Width*Height*3];

        for (int y = 0; y < Height; y++) {
            float[] scanline = ReadScanline(stream, Width);
            /// HDR scanlines are stored top-to-bottom; flip to match standard
            /// bottom-left-origin GL texture coordinates.
            int destRow = Height - 1 - y;
            Array.Copy(scanline, 0, Data, destRow*Width*3, Width*3);
        }
    }

    private static void ReadHeader (Stream stream, out int width, out int height) {
        /// Header is plain ASCII lines terminated by "\n", ending with a blank
        /// line, then a single "-Y height +X width" resolution line.
        string? line;
        while ((line = ReadLine(stream)) != null) {
            if (line.Length == 0) break; /// blank line ends the header proper
        }

        string? resLine = ReadLine(stream) ?? throw new InvalidDataException("Missing HDR resolution line.");
        string[] tokens = resLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != 4 || tokens[0] != "-Y" || tokens[2] != "+X")
            throw new NotSupportedException($"Unsupported HDR orientation: \"{resLine}\". Only -Y H +X W is supported.");

        height = int.Parse(tokens[1]);
        width = int.Parse(tokens[3]);
    }

    private static string? ReadLine (Stream stream) {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        int b;
        bool any = false;
        while ((b = stream.ReadByte()) != -1) {
            any = true;
            if (b == '\n') return sb.ToString();
            sb.Append((char)b);
        }
        return any ? sb.ToString() : null;
    }

    private static float[] ReadScanline (Stream stream, int width) {
        byte[] rgbe = new byte[width*4];

        /// New-format RLE scanlines start with a 4-byte marker: 2,2,hi,lo
        /// where (hi<<8)|lo == width. Anything else falls back to old-format
        /// flat (uncompressed) or per-pixel RLE, which we don't expect to
        /// see from modern exporters but guard against anyway.
        byte[] marker = ReadExact(stream, 4);

        if (width is >= 8 and <= 0x7fff && marker[0] == 2 && marker[1] == 2 && ((marker[2] << 8) | marker[3]) == width) {
            /// New-format RLE: each of the 4 channels (R,G,B,E) is stored
            /// separately across the full scanline width, each as a run of
            /// either a flat-repeat or literal-copy sequence.
            for (int channel = 0; channel < 4; channel++) {
                int x = 0;
                while (x < width) {
                    int count = stream.ReadByte();
                    if (count <= 0) throw new EndOfStreamException("Unexpected end of HDR scanline.");

                    if (count > 128) {
                        /// Run of (count-128) identical bytes.
                        count -= 128;
                        byte value = ReadByteChecked(stream);
                        for (int i = 0; i < count; i++)
                            rgbe[(x + i)*4 + channel] = value;
                    } else {
                        /// Literal run of `count` distinct bytes.
                        for (int i = 0; i < count; i++)
                            rgbe[(x + i)*4 + channel] = ReadByteChecked(stream);
                    }
                    x += count;
                }
            }
        } else {
            /// Old-format / flat scanline: the 4 bytes we already read are
            /// pixel 0, the rest follow directly, uncompressed.
            rgbe[0] = marker[0];
            rgbe[1] = marker[1];
            rgbe[2] = marker[2];
            rgbe[3] = marker[3];
            byte[] rest = ReadExact(stream, (width - 1)*4);
            Array.Copy(rest, 0, rgbe, 4, rest.Length);
        }

        float[] result = new float[width*3];
        for (int x = 0; x < width; x++) {
            byte r = rgbe[x*4 + 0];
            byte g = rgbe[x*4 + 1];
            byte b = rgbe[x*4 + 2];
            byte e = rgbe[x*4 + 3];

            if (e == 0) {
                result[x*3 + 0] = 0f;
                result[x*3 + 1] = 0f;
                result[x*3 + 2] = 0f;
            } else {
                /// RGBE -> float: mantissa/256 * 2^(exponent-128).
                float scale = MathF.Pow(2f, e - 128 - 8);
                result[x*3 + 0] = r*scale;
                result[x*3 + 1] = g*scale;
                result[x*3 + 2] = b*scale;
            }
        }
        return result;
    }

    private static byte[] ReadExact (Stream stream, int count) {
        byte[] buffer = new byte[count];
        int read = 0;
        while (read < count) {
            int n = stream.Read(buffer, read, count - read);
            if (n == 0) throw new EndOfStreamException("Unexpected end of HDR file.");
            read += n;
        }
        return buffer;
    }

    private static byte ReadByteChecked (Stream stream) {
        int b = stream.ReadByte();
        if (b == -1) throw new EndOfStreamException("Unexpected end of HDR file.");
        return (byte)b;
    }



    /// Bakes linear HDR floats (RGB) into 8-bit sRGB RGBA: exposure, then PBR Neutral, then sRGB encode.
    public static byte[] BakeLdrAces (float[] data, int width, int height, float exposure) {
        byte[] result = new byte[width*height*4];
        Parallel.For(0, height, y => {
            for (int x = 0; x < width; x++) {
                int i = (y*width + x)*3;
                int o = (y*width + x)*4;

                /// dither in the 8-bit domain, hides banding in the sun falloff
                float n = Hash(x, y) - 0.5f;

                result[o] = ToSrgb8(Aces(data[i]*exposure), n);
                result[o + 1] = ToSrgb8(Aces(data[i + 1]*exposure), n);
                result[o + 2] = ToSrgb8(Aces(data[i + 2]*exposure), n);
                result[o + 3] = 255;
            }
        });
        return result;
    }

    /// per-channel ACES (Narkowicz): bright warm values clip toward white like the Poly Haven JPGs
    private static float Aces (float x) {
        return Math.Clamp((x*(2.51f*x + 0.03f))/(x*(2.43f*x + 0.59f) + 0.14f), 0f, 1f);
    }

    private static float Hash (int x, int y) {
        uint h = (uint)(x*73856093 ^ y*19349663);
        h = (h ^ (h >> 13))*1274126177u;
        return ((h ^ (h >> 16)) & 0xFFFFFF)/(float)0x1000000;
    }

    private static byte ToSrgb8 (float c, float dither) {
        c = Math.Clamp(c, 0f, 1f);
        c = c <= 0.0031308f ? 12.92f*c : 1.055f*MathF.Pow(c, 1f/2.4f) - 0.055f;
        return (byte)Math.Clamp(c*255f + 0.5f + dither, 0f, 255f);
    }


    /// Bakes linear HDR floats (RGB) into 8-bit sRGB RGBA: exposure, then PBR Neutral, then sRGB encode.
    public static byte[] BakeLdrNeutral (float[] data, int width, int height, float exposure) {
        byte[] result = new byte[width*height*4];
        Parallel.For(0, height, y => {
            for (int x = 0; x < width; x++) {
                int i = (y*width + x)*3;
                float r = data[i]*exposure;
                float g = data[i + 1]*exposure;
                float b = data[i + 2]*exposure;
                PbrNeutral(ref r, ref g, ref b);

                int o = (y*width + x)*4;
                result[o] = ToSrgb8(r);
                result[o + 1] = ToSrgb8(g);
                result[o + 2] = ToSrgb8(b);
                result[o + 3] = 255;
            }
        });
        return result;
    }

    /// Same curve as the PBRNeutral in the resolve shader.
    private static void PbrNeutral (ref float r, ref float g, ref float b) {
        const float startCompression = 0.8f - 0.04f;
        const float desaturation = 0.15f;
        float x = MathF.Min(r, MathF.Min(g, b));
        float offset = x < 0.08f ? x - 6.25f*x*x : 0.04f;
        r -= offset;
        g -= offset;
        b -= offset;

        float peak = MathF.Max(r, MathF.Max(g, b));
        if (peak < startCompression) return;

        const float d = 1f - startCompression;
        float newPeak = 1f - d*d/(peak + d - startCompression);
        float s = newPeak/peak;
        r *= s;
        g *= s;
        b *= s;

        float k = 1f - 1f/(desaturation*(peak - newPeak) + 1f);
        r += (newPeak - r)*k;
        g += (newPeak - g)*k;
        b += (newPeak - b)*k;
    }

    private static byte ToSrgb8 (float c) {
        c = Math.Clamp(c, 0f, 1f);
        c = c <= 0.0031308f ? 12.92f*c : 1.055f*MathF.Pow(c, 1f/2.4f) - 0.055f;
        return (byte)(c*255f + 0.5f);
    }

}