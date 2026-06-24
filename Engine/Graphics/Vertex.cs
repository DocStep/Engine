using System.Numerics;

namespace Engine.Graphics;


public struct Vertex {
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 UV;

    public Vertex (Vector3 position, Vector3 normal, Vector2 uv) {
        Position = position;
        Normal = normal;
        UV = uv;
    }

    /// Number of floats per vertex (3 position + 3 normal + 2 uv).
    public const uint FloatStride = 8;
}
