using Newtonsoft.Json.Linq;

namespace Engine;


public interface IAsset : IDisposable {
    public string Name { get; protected set; }
    public long Id { get; protected set; }
    public string? Path { get; set; }

    public void Save (string path);
}

/// Asset type that knows how to load itself from a path
public interface IAsset<T> : IAsset where T : IAsset<T> {
    public static abstract T? Load (string path);
}
