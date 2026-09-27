namespace Engine;


/// Implemented by components that own their data outside the generic per-field JSON block
/// (e.g. ChunksGrid, whose cells live in separate chunk files) and want full control over
/// their own load instead of the reflection field-fill pass.
public interface ICustomLoad<TSelf> where TSelf : Component, ICustomLoad<TSelf> {
    static abstract TSelf? Load (string path, int part = 100);
}
