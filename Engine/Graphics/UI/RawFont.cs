namespace Engine.Graphics.UI;


/// Raw font file bytes, loaded once and reused to bake atlases at any size
public class RawFont : IAsset<RawFont> {

    public string Name { get; set; } = null!;
    public long Id { get; set; }
    public string? Path { get; set; }

    public byte[] Data { get; private set; } = null!;


    public void Save (string path) {
        File.WriteAllBytes(path, Data);
    }

    public static RawFont Load (string path) {
        return new RawFont {
            Name = System.IO.Path.GetFileName(path),
            Path = path,
            Data = File.ReadAllBytes(path),
        };
    }

    public void Dispose () {
        /// no GPU/unmanaged resources to release
    }

}
