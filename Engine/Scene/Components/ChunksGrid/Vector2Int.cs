using System;
using System.Collections.Generic;
using System.Text;

namespace Engine;


public struct Vector2Int : IEquatable<Vector2Int> {
    public Vector2Int (int x, int y) {
        X = x;
        Y = y;
    }

    public int X;
    public int Y;

    public static readonly Vector2Int Zero = new Vector2Int(0, 0);
    public static readonly Vector2Int One = new Vector2Int(1, 1);
    public static readonly Vector2Int Up = new Vector2Int(0, 1);
    public static readonly Vector2Int Down = new Vector2Int(0, -1);
    public static readonly Vector2Int Left = new Vector2Int(-1, 0);
    public static readonly Vector2Int Right = new Vector2Int(1, 0);


    public float Length => MathF.Sqrt(X * X + Y * Y);
    public int LengthSquared => X * X + Y * Y;

    public static Vector2Int operator + (Vector2Int a, Vector2Int b) => new Vector2Int(a.X + b.X, a.Y + b.Y);
    public static Vector2Int operator - (Vector2Int a, Vector2Int b) => new Vector2Int(a.X - b.X, a.Y - b.Y);
    public static Vector2Int operator - (Vector2Int a) => new Vector2Int(-a.X, -a.Y);
    public static Vector2Int operator * (Vector2Int a, int s) => new Vector2Int(a.X * s, a.Y * s);
    public static Vector2Int operator * (int s, Vector2Int a) => new Vector2Int(a.X * s, a.Y * s);
    public static Vector2Int operator / (Vector2Int a, int s) => new Vector2Int(a.X / s, a.Y / s);

    public static bool operator == (Vector2Int a, Vector2Int b) => a.X == b.X && a.Y == b.Y;
    public static bool operator != (Vector2Int a, Vector2Int b) => !(a == b);

    public static explicit operator Vector2 (Vector2Int v) => new Vector2(v.X, v.Y);
    public static explicit operator Vector3 (Vector2Int v) => new Vector3(v.X, v.Y, 0);

    public bool Equals (Vector2Int other) => X == other.X && Y == other.Y;
    public override bool Equals (object obj) => obj is Vector2Int v && Equals(v);
    public override int GetHashCode () => HashCode.Combine(X, Y);
    public override string ToString () => $"({X}, {Y})";

}
