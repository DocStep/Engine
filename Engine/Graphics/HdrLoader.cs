namespace Engine.Graphics;


/// Loads Radiance (.hdr) images — the de-facto standard format for HDR
/// environment maps (Poly Haven, etc.). Returns flat linear-RGB float data
/// ready to upload as a GL texture; no cubemap conversion happens here,
/// the image stays equirectangular.
public static class HdrLoader {
    public static (float[] Data, int Width, int Height) Load (string path) {
        using var stream = File.OpenRead(path);

        int width = 0, height = 0;
        ReadHeader(stream, out width, out height);

        var data = new float[width*height*3];

        for (int y = 0; y < height; y++) {
            float[] scanline = ReadScanline(stream, width);
            /// HDR scanlines are stored top-to-bottom; flip to match standard
            /// bottom-left-origin GL texture coordinates.
            int destRow = height - 1 - y;
            Array.Copy(scanline, 0, data, destRow*width*3, width*3);
        }

        return (data, width, height);
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
        var sb = new System.Text.StringBuilder();
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
        var rgbe = new byte[width*4];

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

        var result = new float[width*3];
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
        var buffer = new byte[count];
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
}