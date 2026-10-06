using Silk.NET.OpenAL;
using Newtonsoft.Json;

namespace Engine.Audio;


/// <summary>Decoded PCM data uploaded to an OpenAL buffer.</summary>
public class AudioClip : IAsset<AudioClip> {

    public string Name { get; set; } = "";
    [Readonly] public long Id { get; set; } = Lib.Id;
    [Hide] public string? Path { get; set; }

    [Hide, JsonIgnore] public uint Buffer;
    [Readonly] public float Duration;
    [Readonly] public int Channels;
    [Readonly] public int SampleRate;

    bool disposed;


    /// <summary>`part` is unused, a clip is always loaded whole.</summary>
    public static AudioClip? Load (string path, int part = 100) {
        if (!File.Exists(path)) return null;

        var (data, channels, rate) = ReadWavPcm16(path);
        var format = channels == 1 ? BufferFormat.Mono16 : BufferFormat.Stereo16;
        var al = AudioManager.Instance.AL;

        uint buf = al.GenBuffer();
        unsafe {
            fixed (short* p = data)
                al.BufferData(buf, format, p, data.Length*sizeof(short), rate);
        }

        return new AudioClip {
            Name = System.IO.Path.GetFileNameWithoutExtension(path),
            Path = path,
            Buffer = buf,
            Channels = channels,
            SampleRate = rate,
            Duration = data.Length / (float)(channels*rate)
        };
    }

    /// <summary>The clip is immutable, so saving copies the source file.</summary>
    public void Save (string path) {
        if (Path == null || !File.Exists(Path)) throw new Exception("Source file missing: " + Path);
        if (System.IO.Path.GetFullPath(Path) != System.IO.Path.GetFullPath(path)) File.Copy(Path, path, true);
        Path = path;
    }

    /// <summary>Loads PCM WAV (8/16/24/32-bit int, 32-bit float), converted to 16-bit.</summary>
    static (short[] data, int channels, int sampleRate) ReadWavPcm16 (string path) {
        using var br = new BinaryReader(File.OpenRead(path));
        if (new string(br.ReadChars(4)) != "RIFF") throw new Exception("Not RIFF: " + path);
        br.ReadInt32();
        if (new string(br.ReadChars(4)) != "WAVE") throw new Exception("Not WAVE: " + path);

        int tag = 0, channels = 0, rate = 0, bits = 0;
        byte[]? raw = null;

        while (br.BaseStream.Position + 8 <= br.BaseStream.Length) {
            string id = new string(br.ReadChars(4));
            int size = br.ReadInt32();
            long next = br.BaseStream.Position + size + (size & 1); // chunks are word-aligned

            if (id == "fmt ") {
                tag = br.ReadUInt16();
                channels = br.ReadUInt16();
                rate = br.ReadInt32();
                br.ReadInt32(); br.ReadUInt16();
                bits = br.ReadUInt16();
                if (tag == 0xFFFE && size >= 26) {
                    br.ReadUInt16(); br.ReadUInt16(); br.ReadInt32();
                    tag = br.ReadUInt16(); // sub-format GUID's first 2 bytes: 1=PCM, 3=float
                }
            } else if (id == "data") {
                raw = br.ReadBytes(size);
            }
            br.BaseStream.Position = next;
        }
        if (raw == null) throw new Exception("No data chunk: " + path);

        int bps = bits / 8, n = raw.Length / bps;
        var pcm = new short[n];
        for (int i = 0; i < n; i++) {
            int o = i*bps;
            pcm[i] = (tag, bits) switch {
                (1, 8) => (short)((raw[o] - 128) << 8),
                (1, 16) => BitConverter.ToInt16(raw, o),
                (1, 24) => (short)((raw[o + 2] << 8) | raw[o + 1]),
                (1, 32) => (short)(BitConverter.ToInt32(raw, o) >> 16),
                (3, 32) => (short)(Math.Clamp(BitConverter.ToSingle(raw, o), -1f, 1f)*short.MaxValue),
                _ => throw new Exception($"Unsupported WAV: tag={tag} bits={bits}")
            };
        }
        return (pcm, channels, rate);
    }

    public void Dispose () {
        if (disposed) return;
        disposed = true;
        AudioManager.Instance.AL.DeleteBuffer(Buffer);
        GC.SuppressFinalize(this);
    }

}
