namespace Engine;


public interface IAsset : IDisposable {
    string Name { get; }
}

/// Asset type that knows how to load itself from a path
public interface IAsset<T> : IAsset where T : IAsset<T> {
    static abstract T Load (string path);
}
