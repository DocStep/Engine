namespace Engine.Graphics;


public struct Vertex (Vector3 position, Vector3 normal, Vector2 uv) {
    public Vector3 Position = position;
    public Vector3 Normal = normal;
    public Vector2 UV = uv;

    /// Number of floats per vertex (3 position + 3 normal + 2 uv).
    public const uint FloatStride = 8;
}
