using Silk.NET.Maths;

namespace Engine.Graphics;


public struct Vertex {
    public Vector3D<float> Position;
    public Vector3D<float> Normal;
    public Vector2D<float> UV;

    public Vertex (Vector3D<float> position, Vector3D<float> normal, Vector2D<float> uv) {
        Position = position;
        Normal = normal;
        UV = uv;
    }

    /// Number of floats per vertex (3 position + 3 normal + 2 uv).
    public const uint FloatStride = 8;
}
