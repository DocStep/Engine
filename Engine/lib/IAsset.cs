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


/// One flat entry in a saved file — a GameObject, a Transform, or any Component
public class Block {
    public long Id;
    public string Type = null!; /// "GameObject", "Transform", "ChunksGrid", etc.
    public JObject Data = null!; /// the object's own fields; refs to other blocks stored as ids
}

public class PrefabFile {
    public List<Block> Blocks = new List<Block>();
}
