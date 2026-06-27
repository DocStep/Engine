using System;
using System.Numerics;
using System.Text;

namespace Engine;


public struct Ray {
    public Vector3 Origin;
    public Vector3 Direction;

    public Ray (Vector3 origin, Vector3 direction) {
        Origin = origin;
        Direction = direction;
    }
}
