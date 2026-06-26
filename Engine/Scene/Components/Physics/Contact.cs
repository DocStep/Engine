using System.Numerics;

namespace Engine;


public struct Contact {
    public Vector3 normal;
    public float penetration;
    public Vector3 point;
}
