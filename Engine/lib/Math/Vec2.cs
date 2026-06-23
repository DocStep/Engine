using System;


public struct Vec2 {
    public Vec2 (float x, float y) {
        this.x = x;
        this.y = y;
    }

    public float x;
    public float y;


    public static readonly Vec2 zero = new Vec2(0f, 0f);
    public static readonly Vec2 one = new Vec2(1f, 1f);
    public static readonly Vec2 up = new Vec2(0f, 1f);
    public static readonly Vec2 down = new Vec2(0f, -1f);
    public static readonly Vec2 left = new Vec2(-1f, 0f);
    public static readonly Vec2 right = new Vec2(1f, 0f);


    public static Vec2 operator + (Vec2 a, Vec2 b) => new Vec2(a.x + b.x, a.y + b.y);
    public static Vec2 operator - (Vec2 a, Vec2 b) => new Vec2(a.x - b.x, a.y - b.y);
    public static Vec2 operator * (Vec2 v, float s) => new Vec2(v.x*s, v.y*s);
    public static Vec2 operator / (Vec2 v, float s) => new Vec2(v.x/s, v.y/s);

    public static bool operator == (Vec2 a, Vec2 b) => a.x == b.x && a.y == b.y;
    public static bool operator != (Vec2 a, Vec2 b) => !(a == b);


    public override bool Equals (object? obj) => obj is Vec2 v && this == v;

    public override int GetHashCode () => HashCode.Combine(x, y);

}
