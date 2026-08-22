namespace Engine.Graphics;


public struct Vertex2D (Vector2 position, Vector2 uv) {
    public Vector2 Position = position;
    public Vector2 UV = uv;

    public const uint FloatStride = 4;
}
