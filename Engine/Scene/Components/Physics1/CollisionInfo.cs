using System.Numerics;

namespace Engine.Bad;


public struct CollisionInfo {
    public Vector3 normal;
    public float penetration;
    public ColliderComponent a;
    public ColliderComponent b;
}
