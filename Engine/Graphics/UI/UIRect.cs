namespace Engine.Graphics.UI;


public struct UIRect {
    public float X;
    public float Y;
    public float Width;
    public float Height;

    /// Returns true if point is inside this rect
    public bool Contains (float px, float py) {
        return X <= px && px <= X + Width && Y <= py && py <= Y + Height;
    }
}
