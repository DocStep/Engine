using System;
using System.Numerics;

namespace Engine;


public struct Manifold {
    public bool Colliding;
    public Vector3 Normal; /// points from A toward B
    public int ContactCount;
    public Contact C0, C1, C2, C3;

    public void Add (Vector3 point, float penetration) {
        switch (ContactCount) {
            case 0: C0 = new Contact { Point = point, Penetration = penetration }; break;
            case 1: C1 = new Contact { Point = point, Penetration = penetration }; break;
            case 2: C2 = new Contact { Point = point, Penetration = penetration }; break;
            case 3: C3 = new Contact { Point = point, Penetration = penetration }; break;
        }
        if (ContactCount < 4) ContactCount++;
    }

    public Contact this[int i] => i switch { 0 => C0, 1 => C1, 2 => C2, _ => C3 };
}

