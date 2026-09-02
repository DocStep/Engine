namespace Engine;


public static class VectorMath {

    public static void Add (ref Vector2 a, in Vector2 b) { a.X += b.X; a.Y += b.Y; }
    public static void Add (ref Vector3 a, in Vector3 b) { a.X += b.X; a.Y += b.Y; a.Z += b.Z; }

    public static void Subtract (ref Vector2 a, in Vector2 b) { a.X -= b.X; a.Y -= b.Y; }
    public static void Subtract (ref Vector3 a, in Vector3 b) { a.X -= b.X; a.Y -= b.Y; a.Z -= b.Z; }

    public static void Multiply (ref Vector2 a, float scalar) { a.X *= scalar; a.Y *= scalar; }
    public static void Multiply (ref Vector3 a, float scalar) { a.X *= scalar; a.Y *= scalar; a.Z *= scalar; }

    public static void Divide (ref Vector2 a, float scalar) { a.X /= scalar; a.Y /= scalar; }
    public static void Divide (ref Vector3 a, float scalar) { a.X /= scalar; a.Y /= scalar; a.Z /= scalar; }

    public static Vector3 ToVector3 (this Vector2 v) => new Vector3(v.X, v.Y, 0);
    public static Vector2 ToVector2 (this Vector3 v) => new Vector2(v.X, v.Y);

}
