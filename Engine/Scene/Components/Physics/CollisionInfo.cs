using System.Numerics;

namespace Engine;


public struct CollisionInfo {
    public Vector3 normal;
    public float penetration;
    public ColliderComponent a;
    public ColliderComponent b;
}
